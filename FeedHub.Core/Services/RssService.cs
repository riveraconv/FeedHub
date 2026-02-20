using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

public class RssService : IRssService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient; // Inyectamos HttpClient para usar el token

    public RssService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<NewsItem>> GetNewsAsync(string feedUrl, string categoryFromDict, CancellationToken ct = default)
    {
        var news = new List<NewsItem>();
        if (string.IsNullOrEmpty(feedUrl)) return news;

        try
        {
            // CREAMOS LA PETICIÓN LOCAL (Esto es Thread-Safe, cada hilo tiene la suya)
            var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);

            // Añadimos las cabeceras directamente a la petición, no al cliente global
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
            request.Headers.TryAddWithoutValidation("Accept", "text/xml,application/xml,application/rss+xml,*/*");
            request.Headers.Referrer = new Uri("https://www.google.com/");

            _logger.Info($"Downloading: {feedUrl}");

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.Warn($"403 Forbidden: {feedUrl}");
                return news;
            }

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var settings = new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore };

            using var reader = XmlReader.Create(stream, settings);
            var feed = SyndicationFeed.Load(reader);

            if (feed != null)
            {
                foreach (var item in feed.Items.Take(20))
                {
                    ct.ThrowIfCancellationRequested();

                    var link = item.Links.FirstOrDefault()?.Uri.ToString() ?? item.Id;
                    if (string.IsNullOrEmpty(link)) continue;

                    if (categoryFromDict == "technology" && feedUrl.Contains("hipertextual.com"))
                    {
                        string titleLower = item.Title?.Text.ToLower() ?? "";

                        string[] blacklist = { "crítica", "película", "serie", "estreno", "cine", "Disney", "Netflix", "HBO", "tráiler",
                                                "Disney+", "Movistar", "Movistar+", "Prime Video", "Marvel", "Star Wars"};

                        bool hasEntertainmentCategory = item.Categories.Any(c =>
                        c.Name.ToLower().Contains("cine") || c.Name.ToLower().Contains("series"));

                        if (blacklist.Any(k => titleLower.Contains(k)) || hasEntertainmentCategory)
                        {
                            continue;
                        }
                    }

                    news.Add(new NewsItem
                    {
                        Title = StripHtml(item.Title?.Text ?? "Sin título"),
                        Link = link,
                        Description = StripHtml(item.Summary?.Text ?? ""),
                        PublishDate = item.PublishDate.DateTime == DateTime.MinValue
                                        ? item.LastUpdatedTime.DateTime
                                        : item.PublishDate.DateTime,
                        Category = categoryFromDict ?? "General", // Evitamos null en la Key
                        Source = feed.Title?.Text ?? new Uri(feedUrl).Host,
                        ImageUrl = ExtractImageUrl(item)
                    });
                }
            }
        }

        catch (Exception ex)
        {
            _logger.Error($"Error en {feedUrl}: {ex.Message}");
        }

        return news;
    }

    private string? ExtractImageUrl(SyndicationItem item)
    {
        // 1. media:content
        var media = item.ElementExtensions
            .Where(e => e.OuterName == "content" && e.OuterNamespace == "http://search.yahoo.com/mrss/")
            .Select(e => { try { return e.GetObject<XElement>(); } catch { return null; } })
            .Where(x => x != null && x.Attribute("url") != null)
            .OrderByDescending(x => int.TryParse(x.Attribute("width")?.Value, out var w) ? w : 0)
            .FirstOrDefault();

        if (media != null) return media.Attribute("url")?.Value;

        // 2. media:thumbnail
        var thumb = item.ElementExtensions
            .Where(e => e.OuterName == "thumbnail" && e.OuterNamespace == "http://search.yahoo.com/mrss/")
            .Select(e => { try { return e.GetObject<XElement>(); } catch { return null; } })
            .FirstOrDefault(x => x?.Attribute("url") != null);

        if (thumb != null) return thumb.Attribute("url")?.Value;

        // 3. Enclosures
        var enclosure = item.Links.FirstOrDefault(l =>
                        l.RelationshipType == "enclosure" &&
                        (l.MediaType?.StartsWith("image/") ?? false));

        if (enclosure != null) return enclosure.Uri.ToString();

        // 4. METADATOS ESPECÍFICOS (WordPress/EFE)
        var extraImage = item.ElementExtensions
            .FirstOrDefault(e => e.OuterName == "featured-image" || e.OuterName == "image")
            ?.GetObject<XElement>().Value;

        if (!string.IsNullOrEmpty(extraImage) && extraImage.StartsWith("http")) return extraImage;

        // 5. EXTRACCIÓN DE CONTENIDO CODIFICADO (Mejorada)
        string encodedContent = "";
        var encodedExtension = item.ElementExtensions.FirstOrDefault(e =>
            e.OuterName == "encoded" && e.OuterNamespace == "http://purl.org/rss/1.0/modules/content/");

        if (encodedExtension != null)
        {
            // Usamos GetObject para evitar problemas con XmlReader si el XML es irregular
            encodedContent = encodedExtension.GetObject<XElement>().Value;
        }

        string rawHtml = System.Net.WebUtility.HtmlDecode(
            (item.Summary?.Text ?? "") +
            ((item.Content as TextSyndicationContent)?.Text ?? "") +
            encodedContent);

        if (string.IsNullOrWhiteSpace(rawHtml)) return null;

        // 6. REGEX FLEXIBLE (Captura src y data-src con o sin protocolo)
        // Eliminamos el https? obligatorio para que sea más permisivo
        var match = Regex.Match(rawHtml, @"(?:src|data-src)=[""'](?<url>[^""']+\.(?:jpg|jpeg|png|webp|avif)[^""']*)[""']", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string url = match.Groups["url"].Value;

            // Normalización de protocolo
            if (url.StartsWith("//")) url = "https:" + url;

            // Filtro de calidad (tamaño de URL y píxeles)
            if (url.Contains("pixel") || url.Contains("1x1") || url.Length < 15) return null;

            return url;
        }

        return null;
    }

    private string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        input = Regex.Replace(input, "<.*?>", string.Empty, RegexOptions.Singleline);
        input = System.Net.WebUtility.HtmlDecode(input);
        input = Regex.Replace(input, @"\s{2,}", " ").Trim();
        return Regex.Replace(input, @"(Leer(\s+más)?|Ver(\s+más)?|Sigue\s+leyendo)\s*$", "", RegexOptions.IgnoreCase).Trim();
    }
}

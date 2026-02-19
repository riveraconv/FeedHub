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

    // He movido la lógica de la imagen a un método aparte para limpiar el código principal
    private string? ExtractImageUrl(SyndicationItem item)
    {
        // 1. media:content
        var media = item.ElementExtensions
            .Where(e => e.OuterName == "content" && e.OuterNamespace == "http://search.yahoo.com/mrss/")
            .Select(e => { try { return e.GetObject<XElement>(); } catch { return null; }})
            .Where(x => x != null && x.Attribute("url") != null)
            .OrderByDescending(x => int.TryParse(x.Attribute("width")?.Value, out var w) ? w : 0)
            .FirstOrDefault();

        if (media != null) return media.Attribute("url")?.Value;

        // 2.media:thumbnail (for example, BBC)
        var thumb = item.ElementExtensions
            .Where(e => e.OuterName == "thumbnail" && e.OuterNamespace == "http://search.yahoo.com/mrss/")
            .Select(e => {try { return e.GetObject<XElement>(); } catch { return null; }})
            .FirstOrDefault(x => x?.Attribute("url") != null);

        if (thumb != null) return thumb.Attribute("url")?.Value;

        // 3. Enclosures
        var enclosure = item.Links.FirstOrDefault(l => 
                        l.RelationshipType == "enclosure" &&
                        (l.MediaType?.StartsWith("image/") ?? false));

        if (enclosure != null) return enclosure.Uri.ToString();

        // 4. Regex Fallback

        // 4. EFE SPECIFIC SCAN
        string summary = item.Summary?.Text ?? "";
        string content = (item.Content as TextSyndicationContent)?.Text ?? "";

        // Buscamos específicamente en la extensión "description" si el Summary falló
        // EFE a veces duplica la descripción aquí pero con el HTML completo
        string extraDescription = item.ElementExtensions
            .FirstOrDefault(e => e.OuterName == "description")
            ?.GetObject<XElement>().Value ?? "";

        string encodedContent = item.ElementExtensions
            .FirstOrDefault(e => e.OuterName == "encoded" && e.OuterNamespace == "http://purl.org/rss/1.0/modules/content/")
            ?.GetObject<XElement>().Value ?? "";

        // Sumamos todo el contenido bruto para el Regex
        string rawHtml = summary + content + encodedContent + extraDescription;

        if (!string.IsNullOrEmpty(rawHtml))
        {
            // Regex robusto para EFE: busca cualquier etiqueta img y extrae el src
            var match = Regex.Match(rawHtml, @"<img[^>]+src=[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string url = match.Groups["url"].Value;

                // Limpieza de entidades HTML (EFE suele codificar las URLs)
                url = System.Net.WebUtility.HtmlDecode(url);

                if (url.StartsWith("//")) url = "https:" + url;

                // EFE a veces mete imágenes de 1x1 o píxeles de rastreo. 
                // Si la URL es muy corta o sospechosa, la ignoramos.
                if (url.Contains("pixel") || url.Contains("/1x1")) return null;

                return url;
            }
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

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
        HttpResponseMessage? response = null;

        try
        {
            // 1. Limpieza segura
            _httpClient.DefaultRequestHeaders.Clear();

            //1.5 DISFRAZ DINAMICO
            var uri = new Uri(feedUrl);
            _httpClient.DefaultRequestHeaders.Referrer = new Uri($"{uri.Scheme}://{uri.Host}");

            // 2. Usamos un User-Agent sencillo pero moderno (sin tantas "pistas" que complican la Key)
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/xml, application/xml, application/rss+xml, */*");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "es-ES,es;q=0.9");
            _logger.Info($"Downloading feed from {feedUrl}");

            // 2. Descarga
            response = await _httpClient.GetAsync(feedUrl, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.Warn($"Acceso denegado (403) en {feedUrl}. Saltando fuente.");
                return news;
            }

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);

            // CONFIGURACIÓN PARA EVITAR EL ERROR DE DTD
            var settings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Parse, // Permite leer feeds con declaraciones DTD
                IgnoreWhitespace = true
            };

            // 2. Cargamos el Feed desde el stream descargado
            using var reader = XmlReader.Create(stream, settings);
            var feed = SyndicationFeed.Load(reader);

            if (feed == null) return news;

            _logger.Info($"Feed downloaded successfully. Items: {feed.Items.Count()}");

            foreach (var item in feed.Items.Take(20))
            {
                // Verificamos si se ha solicitado cancelar en cada iteración del bucle
                ct.ThrowIfCancellationRequested();
                var categoryName = item.Categories.FirstOrDefault()?.Name ?? "General";
                string? imageUrl = ExtractImageUrl(item);

                news.Add(new NewsItem
                {
                    Title = StripHtml(item.Title?.Text ?? ""),
                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    Description = StripHtml(item.Summary?.Text ?? item.Copyright?.Text ?? ""),
                    PublishDate = item.PublishDate.DateTime == DateTime.MinValue
                                  ? item.LastUpdatedTime.DateTime
                                  : item.PublishDate.DateTime,
                    Category = categoryFromDict,
                    Source = feed.Title?.Text ?? new Uri(feedUrl).Host,
                    ImageUrl = imageUrl
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Download timed out for: {feedUrl}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading RSS feed {feedUrl}: {ex.Message}");
        }
        finally
        {
            response?.Dispose();
        }

        return news;
    }

    // He movido la lógica de la imagen a un método aparte para limpiar el código principal
    private string? ExtractImageUrl(SyndicationItem item)
    {
        // 1. media:content
        var media = item.ElementExtensions
            .Where(e => e.OuterName == "content" && e.OuterNamespace == "http://search.yahoo.com/mrss/")
            .Select(e =>
            {
                try { return e.GetObject<XElement>(); }
                catch { return null; }
            })
            .Where(x => x != null && x.Attribute("url") != null)
            .OrderByDescending(x => int.TryParse(x.Attribute("width")?.Value, out var w) ? w : 0)
            .FirstOrDefault();

        if (media != null) return media.Attribute("url")?.Value;

        // 2. Enclosures
        var enclosure = item.Links.FirstOrDefault(l => 
                        l.RelationshipType == "enclosure" &&
                        (l.MediaType?.StartsWith("image/") ?? false));

        if (enclosure != null) return enclosure.Uri.ToString();

        // 3. Regex Fallback

        var summary = item.Summary?.Text ?? item.Content?.ToString() ?? "";
        if (!string.IsNullOrEmpty(summary))
        {
            var match = Regex.Match(summary, @"<img.+?src=[""'](.+?)[""']", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;
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

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

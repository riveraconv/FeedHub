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

    public async Task<List<NewsItem>> GetNewsAsync(string feedUrl, CancellationToken ct = default)
    {
        var news = new List<NewsItem>();

        try
        {
            _logger.Info($"Downloading feed from {feedUrl}");

            // 1. Descargamos el XML usando el CancellationToken
            // Si pasan 5 segundos (según el Aggregator), esta línea lanzará la excepción y detendrá el proceso
            using var response = await _httpClient.GetAsync(feedUrl, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);

            // 2. Cargamos el Feed desde el stream descargado
            using var reader = XmlReader.Create(stream);
            var feed = SyndicationFeed.Load(reader);

            if (feed == null) return news;

            _logger.Info($"Feed downloaded successfully. Items: {feed.Items.Count()}");

            foreach (var item in feed.Items.Take(20))
            {
                // Verificamos si se ha solicitado cancelar en cada iteración del bucle
                ct.ThrowIfCancellationRequested();

                string? imageUrl = ExtractImageUrl(item);

                news.Add(new NewsItem
                {
                    Title = StripHtml(item.Title?.Text ?? ""),
                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    Description = StripHtml(item.Summary?.Text ?? item.Copyright?.Text ?? ""),
                    PublishDate = item.PublishDate.DateTime == DateTime.MinValue
                                  ? item.LastUpdatedTime.DateTime
                                  : item.PublishDate.DateTime,
                    Category = item.Categories.FirstOrDefault()?.Name ?? "",
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

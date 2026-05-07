using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using System.Linq.Expressions;
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
    try
    {
        // --- 1. INTERCEPTOR DE ETIQUETAS PERSONALIZADAS (El Confidencial, Ecoticias, EFE) ---
        // Buscamos cualquier extensión que use etiquetas no estándar
        var customExt = item.ElementExtensions
            .FirstOrDefault(e => new[] { "list_image", "image", "fullimage", "featured-image", "content", "thumbnail" }
            .Contains(e.OuterName.ToLower()));

        if (customExt != null)
        {
            var xml = customExt.GetObject<XElement>();
            
            // Intento A: La URL está en el texto de la etiqueta <image>URL</image>
            string val = xml.Value.Trim();
            if (val.StartsWith("http") && Regex.IsMatch(val, @"\.(jpg|jpeg|png|webp|avif)", RegexOptions.IgnoreCase))
                return val;

            // Intento B: La URL está en un atributo <image url="..." /> o src/href
            var attr = xml.Attributes().FirstOrDefault(a => 
                new[] { "url", "src", "href" }.Contains(a.Name.LocalName.ToLower()));
            
            if (attr != null && attr.Value.StartsWith("http")) 
                return attr.Value;
        }

        // --- 2. MEDIA RSS AGNOSTICO (Para Ecoticias y otros con Namespace mal declarado) ---
        // En lugar de filtrar por la URL del Namespace (que puede fallar), buscamos por nombre local
        var mediaElement = item.ElementExtensions
            .Where(e => e.OuterName == "content" || e.OuterName == "thumbnail" || e.OuterName == "group")
            .Select(e => { try { return e.GetObject<XElement>(); } catch { return null; } })
            .FirstOrDefault(x => x != null && (x.Attribute("url") != null || x.Descendants().Any(d => d.Attribute("url") != null)));

        if (mediaElement != null)
        {
            // Buscamos el atributo 'url' en el elemento o en sus hijos (caso <media:group>)
            var url = mediaElement.Attribute("url")?.Value ?? 
                      mediaElement.Descendants().FirstOrDefault(d => d.Attribute("url") != null)?.Attribute("url")?.Value;
            
            if (!string.IsNullOrEmpty(url) && url.StartsWith("http")) return url;
        }

        // --- 3. ENCLOSURES (Estándar de adjuntos) ---
        var enclosure = item.Links.FirstOrDefault(l => 
            l.RelationshipType == "enclosure" && (l.MediaType?.StartsWith("image/") ?? false));
        
        if (enclosure != null) return enclosure.Uri.ToString();

        // --- 4. EXTRACCIÓN MANUAL DE HTML (El Tiempo.es y contenido en CDATA) ---
        string encodedContent = "";
        var encodedExt = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "encoded");
        if (encodedExt != null) encodedContent = encodedExt.GetObject<XElement>().Value;

        // Combinamos Summary, Content y Encoded para no dejarnos nada
        string rawHtml = System.Net.WebUtility.HtmlDecode(
            (item.Summary?.Text ?? "") + 
            ((item.Content as TextSyndicationContent)?.Text ?? "") + 
            encodedContent);

        if (!string.IsNullOrWhiteSpace(rawHtml))
        {
            // Regex flexible para pillar src o data-src
            var match = Regex.Match(rawHtml, @"(?:src|data-src|href)=[""'](?<url>https?://[^""']+\.(?:jpg|jpeg|png|webp|avif)[^""']*)[""']", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string url = match.Groups["url"].Value;
                if (!url.Contains("pixel") && url.Length > 20) return url;
            }
        }

        // --- 5. ÚLTIMO RECURSO: ESCANEO BINARIO DE EXTENSIONES (Fuerza Bruta) ---
        // Si Ecoticias tiene la URL en una etiqueta que no conocemos, la buscamos por patrón de texto
        foreach (var ext in item.ElementExtensions)
        {
            try 
            {
                var rawXml = ext.GetObject<XElement>().ToString();
                var fallbackMatch = Regex.Match(rawXml, @"(https?://[^""'\s]+\.(?:jpg|jpeg|png|webp))", RegexOptions.IgnoreCase);
                if (fallbackMatch.Success && !fallbackMatch.Value.Contains("favicon"))
                    return fallbackMatch.Value;
            }
            catch { continue; }
        }
    }
    catch (Exception ex)
    {

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

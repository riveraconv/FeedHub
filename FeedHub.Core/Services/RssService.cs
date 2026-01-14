using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using System.Diagnostics;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using FeedHub_Core.Utilities;

namespace FeedHub_Core.Services
{
    public class RssService : IRssService
    {
        private readonly ILogger _logger;
        public RssService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<NewsItem>> GetNewsAsync(string feedUrl)
        {
            _logger.Info("Worked");
            _logger.Info($"Downloading feed from {feedUrl}");

            var news = new List<NewsItem>();

            try
            {
                using var reader = XmlReader.Create(feedUrl, new XmlReaderSettings { Async = true });
                var feed = SyndicationFeed.Load(reader);

                _logger.Info($"Feed downloaded succesfully. Items: {feed?.Items.Count()}");

                if (feed == null)
                    return news;

                foreach (var item in feed.Items)
                {
                    string? imageUrl = null;

                    try
                    {
                        // 1️⃣ media:content (prioritario)
                        var mediaContents = item.ElementExtensions
                            .Where(e => e.OuterName == "content" &&
                                        e.OuterNamespace == "http://search.yahoo.com/mrss/")
                            .Select(e => e.GetObject<XElement>())
                            .Where(x => x?.Attribute("url") != null)
                            .Select(x => new
                            {
                                Url = x!.Attribute("url")!.Value,
                                Width = int.TryParse(x.Attribute("width")?.Value, out var w) ? w : -1
                            })
                            .OrderByDescending(x => x.Width)
                            .FirstOrDefault();

                        imageUrl = mediaContents?.Url;

                        // 2️⃣ enclosure image/*
                        if (string.IsNullOrWhiteSpace(imageUrl))
                        {
                            var enclosure = item.Links.FirstOrDefault(l =>
                                !string.IsNullOrEmpty(l.MediaType) &&
                                l.MediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase));

                            if (enclosure?.Uri != null)
                                imageUrl = enclosure.Uri.ToString();
                        }

                        // 3️⃣ HTML embebido en Summary (fallback)
                        if (string.IsNullOrWhiteSpace(imageUrl) && !string.IsNullOrEmpty(item.Summary?.Text))
                        {
                            var match = Regex.Match(
                                item.Summary.Text,
                                "<img[^>]+src=[\"']([^\"'>]+)[\"']",
                                RegexOptions.IgnoreCase);

                            if (match.Success)
                                imageUrl = match.Groups[1].Value;
                        }

                        // 4️⃣ Validación final MUY laxa (solo URL válida)
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
                                imageUrl = null;
                        }
                    }
                    catch
                    {
                        imageUrl = null;
                    }
                    news.Add(new NewsItem
                    {
                        Title = StripHtml(item.Title?.Text ?? ""),
                        Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                        Description = StripHtml(item.Summary?.Text ?? ""),
                        PublishDate = item.PublishDate.DateTime,
                        Category = item.Categories.FirstOrDefault()?.Name ?? "",
                        Source = feed.Title?.Text ?? new Uri(feedUrl).Host,
                        ImageUrl = imageUrl
                    });

                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading RSS feed {feedUrl}: {ex.Message}");
            }
            return news;
        }
        private string StripHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Quita todas las etiquetas HTML
            input = Regex.Replace(input, "<.*?>", string.Empty, RegexOptions.Singleline);

            // Decodifica entidades HTML (&amp;, &quot;, etc.)
            input = System.Net.WebUtility.HtmlDecode(input);

            // Elimina espacios repetidos
            input = Regex.Replace(input, @"\s{2,}", " ").Trim();

            // Quita frases residuales típicas al final como "Leer", "Leer más", "Ver más", etc.
            input = Regex.Replace(input, @"(Leer(\s+más)?|Ver(\s+más)?|Sigue\s+leyendo)\s*$",
                                  string.Empty, RegexOptions.IgnoreCase);

            return input.Trim();
        }

    }
}

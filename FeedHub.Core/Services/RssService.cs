using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedHub_Core.Services
{
    public class RssService : IRssService
    {
        public async Task<List<NewsItem>> GetNewsAsync(string feedUrl)
        {
            var news = new List<NewsItem>();

            using var reader = XmlReader.Create(feedUrl, new XmlReaderSettings { Async = true});
            var feed = SyndicationFeed.Load(reader);

            foreach (var item in feed.Items)
            {
                string imageUrl = null;

                try
                {
                    var mediaContents = item.ElementExtensions
                        .Where(e => e.OuterName == "content" &&
                                    e.OuterNamespace == "http://search.yahoo.com/mrss/")
                        .Select(e => e.GetObject<XElement>())
                        .ToList();

                    if (mediaContents.Any())
                    {
                        var bestImage = mediaContents
                            .Select(x => new
                            {
                                Url = x?.Attribute("url")?.Value,
                                Width = int.TryParse(x?.Attribute("width")?.Value, out var w) ? w : 0
                            })
                            .OrderByDescending(x => x.Width)
                            .FirstOrDefault(x => !string.IsNullOrEmpty(x.Url));

                        imageUrl = bestImage?.Url;
                    }

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        var enclosure = item.Links.FirstOrDefault(l =>
                            !string.IsNullOrEmpty(l.MediaType) &&
                            l.MediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase));

                        if (enclosure != null &&
                            enclosure.MediaType != null &&
                            enclosure.MediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                        {
                            imageUrl = enclosure.Uri.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(imageUrl) && !string.IsNullOrEmpty(item.Summary?.Text))
                    {
                        var desc = item.Summary.Text;
                        var start = desc.IndexOf("<img");
                        if (start >= 0)
                        {
                            var urlStart = desc.IndexOf("src=\"", start);
                            if (urlStart > 0)
                            {
                                urlStart += 5;
                                var urlEnd = desc.IndexOf("\"", urlStart);
                                if (urlEnd > urlStart)
                                    imageUrl = desc.Substring(urlStart, urlEnd - urlStart);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                        var uriWithoutQuery = new Uri(imageUrl).AbsolutePath;
                        if (!validExtensions.Any(ext => uriWithoutQuery.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        {
                            imageUrl = null;
                        }
                    }

                }
                catch
                {
                    imageUrl = null;
                }
                news.Add(new NewsItem
                {
                    Title = item.Title?.Text ?? "",
                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    Description = item.Summary?.Text ?? "",
                    PublishDate = item.PublishDate.DateTime,
                    Category = item.Categories.FirstOrDefault()?.Name ?? "",
                    Source = feed.Title?.Text ?? new Uri(feedUrl).Host,
                    ImageUrl = imageUrl
                });
            }
            return await Task.FromResult(news);
        }
    }
}

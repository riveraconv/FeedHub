using FeedHub_Core.Interfaces;
using HtmlAgilityPack;
using System.Net.Http;

namespace FeedHub_Core.Services
{
    public class ArticleReaderService : IArticleReaderService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> GetCleanArticleAsync(string url)
        {
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var articleNode = doc.DocumentNode.SelectSingleNode("//article")
                ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'content')]")
                ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'post')]")
                ?? doc.DocumentNode.SelectSingleNode("//body");

            if (articleNode == null)
                return "Could not extract article content.";

            foreach (var node in articleNode.SelectNodes(".//script|.//style|.//aside|.//footer") ?? Enumerable.Empty<HtmlNode>())
                node.Remove();

            string text = articleNode.InnerText.Trim();
            return System.Net.WebUtility.HtmlDecode(text);
        }
        public string GetCleanArticleSync(string url)
        {
            return GetCleanArticleAsync(url).GetAwaiter().GetResult();
        }
        public async Task<string> GetRawHtmlAsync(string url)
        {
            return await _httpClient.GetStringAsync(url);
        }

    }
}

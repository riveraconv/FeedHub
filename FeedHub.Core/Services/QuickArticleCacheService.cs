using FeedHub_Core.Services;

public class QuickArticleCacheService
{
    private readonly Dictionary<string, CachedArticle> _cache = new();

    private const int CacheMinutes = 30;

    public bool TryGet(string url, out CachedArticle article)
    {
        if (_cache.TryGetValue(url, out article))
        {
            if ((DateTime.Now - article.CachedAt).TotalMinutes < CacheMinutes)
                return true;

            _cache.Remove(url);
        }

        article = null!;
        return false;
    }

    public void Save(string url, QuickArticleResult result)
    {
        _cache[url] = new CachedArticle
        {
            Url = url,
            Title = result.Title,
            Html = result.Html,
            Text = result.Text,
            ImageUrl = result.ImageUrl,
            CachedAt = DateTime.Now
        };
    }
}

public class CachedArticle
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Html { get; set; }
    public string Text { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CachedAt { get; set; }
}

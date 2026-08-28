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

    public void Clear()
    {
        _cache.Clear();
    }
}

public class CachedArticle
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}
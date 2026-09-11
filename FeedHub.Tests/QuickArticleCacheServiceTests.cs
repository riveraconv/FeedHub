using FeedHub_Core.Services;
using System.Reflection;

namespace FeedHub.Tests;

public class QuickArticleCacheServiceTests()
{
    [Fact]
    public void TryGet_ReturnsFalseWhenUrlIsNotCached()
    {
        // ARRANGE

        var service = new QuickArticleCacheService();

        // ACT

        var result = service.TryGet(
            "https://example.com/article",
            out var article);

        // ASSERT

        Assert.False(result);
        Assert.Null(article);

        // Verifies that TryGet returns false when the requested URL is not cached.
    }
    [Fact]
    public void TryGet_ReturnsCachedArticleWhenEntryIsValid()
    {
        // ARRANGE

        var service = new QuickArticleCacheService();

        var article = new QuickArticleResult
        {
            Title = "Título de prueba",
            Html = "<p>Contenido HTML</p>",
            Text = "Contenido de texto",
            ImageUrl = "https://example.com/image.jpg"
        };

        service.Save("https://example.com/article", article);

        // ACT

        var result = service.TryGet(
            "https://example.com/article",
            out var cachedArticle);

        // ASSERT

        Assert.True(result);
        Assert.NotNull(cachedArticle);
        Assert.Equal("https://example.com/article", cachedArticle.Url);
        Assert.Equal(article.Title, cachedArticle.Title);
        Assert.Equal(article.Html, cachedArticle.Html);
        Assert.Equal(article.Text, cachedArticle.Text);
        Assert.Equal(article.ImageUrl, cachedArticle.ImageUrl);

        // Verifies that a valid cached article is retrieved with all its stored properties.
    }
    [Fact]
    public void TryGet_ReturnsFalseAndRemovesArticleWhenEntryIsExpired()
    {
        // ARRANGE

        var service = new QuickArticleCacheService();

        var article = new QuickArticleResult
        {
            Title = "Título de prueba",
            Html = "<p>Contenido HTML</p>",
            Text = "Contenido de texto",
            ImageUrl = "https://example.com/image.jpg"
        };

        service.Save("https://example.com/article", article);

        var cacheField = typeof(QuickArticleCacheService)
            .GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);

        var cache = (Dictionary<string, CachedArticle>)cacheField!.GetValue(service)!;

        cache["https://example.com/article"].CachedAt = DateTime.Now.AddMinutes(-31);

        // ACT

        var result = service.TryGet(
            "https://example.com/article",
            out var cachedArticle);

        // ASSERT

        Assert.False(result);
        Assert.Null(cachedArticle);

        // Verifies that an expired cached article is removed and not returned.
    }
    [Fact]
    public void Clear_RemovesAllCachedArticles()
    {
        // ARRANGE

        var service = new QuickArticleCacheService();

        var article = new QuickArticleResult
        {
            Title = "Título de prueba",
            Html = "<p>Contenido HTML</p>",
            Text = "Contenido de texto",
            ImageUrl = "https://example.com/image.jpg"
        };

        service.Save("https://example.com/article", article);

        // ACT

        service.Clear();

        // ASSERT

        var result = service.TryGet(
            "https://example.com/article",
            out var cachedArticle);

        Assert.False(result);
        Assert.Null(cachedArticle);

        // Verifies that Clear removes all cached articles.
    }
}

using FeedHub_Core.Services;
using System.Net;
using System.Reflection;

namespace FeedHub.Tests;

public class ArticleReaderServiceTests
{
    [Fact]
    public async Task GetCleanArticleAsync_ReturnsArticleContent()
    {
        // ARRANGE

        var rawHtml = """
                      <html>
                          <body>
                              <article>
                                  <p>Este es el contenido principal del artículo.</p>
                              </article>
                          </body>
                      </html>
                      """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetCleanArticleAsync("https://example.com/article");

        // ASSERT

        Assert.Contains("Este es el contenido principal del artículo.", result);

        // Verifies that GetCleanArticleAsync extracts the content from an article element.
    }
    [Fact]
    public async Task GetCleanArticleAsync_UsesContentDivWhenArticleIsNotAvailable()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <body>
                          <div class="content">
                              <p>Este es el contenido principal dentro del contenedor content.</p>
                          </div>
                      </body>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetCleanArticleAsync("https://example.com/article");

        // ASSERT

        Assert.Contains("Este es el contenido principal dentro del contenedor content.", result);

        // Verifies that GetCleanArticleAsync uses a content div when no article element is available.
    }
    [Fact]
    public async Task GetCleanArticleAsync_UsesPostDivWhenPreviousSelectorsAreNotAvailable()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <body>
                          <div class="post">
                              <p>Este es el contenido principal dentro del contenedor post.</p>
                          </div>
                      </body>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetCleanArticleAsync("https://example.com/article");

        // ASSERT

        Assert.Contains("Este es el contenido principal dentro del contenedor post.", result);

        // Verifies that GetCleanArticleAsync uses a post div when previous selectors are unavailable.
    }
    [Fact]
    public async Task GetCleanArticleAsync_UsesBodyWhenPreviousSelectorsAreNotAvailable()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <body>
                          <p>Este es el contenido principal encontrado directamente dentro del body.</p>
                      </body>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetCleanArticleAsync("https://example.com/article");

        // ASSERT

        Assert.Contains("Este es el contenido principal encontrado directamente dentro del body.", result);

        // Verifies that GetCleanArticleAsync uses the body when previous selectors are unavailable.
    }
    [Fact]
    public async Task GetCleanArticleAsync_RemovesScriptFromArticleContent()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el contenido principal del artículo y debe mantenerse.</p>
                              <script>
                                  console.log("Este contenido debe eliminarse.");
                              </script>
                          </article>
                      </body>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetCleanArticleAsync("https://example.com/article");

        // ASSERT

        Assert.Contains("Este es el contenido principal del artículo", result);
        Assert.DoesNotContain("console.log", result);
        Assert.DoesNotContain("Este contenido debe eliminarse", result);

        // Verifies that GetCleanArticleAsync removes script elements from article content.
    }
    [Fact]
    public async Task GetCleanArticleAsync_ReturnsFallbackMessageWhenArticleNodeIsNotFound()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <head>
                          <title>Test</title>
                      </head>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetCleanArticleAsync("https://example.com/article");

        // ASSERT

        Assert.Equal("Could not extract article content.", result);

        // Verifies that the fallback message is returned when no article content node is found.
    }
    [Fact]
    public void GetCleanArticleSync_ReturnsArticleContent()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el contenido del artículo.</p>
                          </article>
                      </body>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = service.GetCleanArticleSync("https://example.com/article");

        // ASSERT

        Assert.Contains("Este es el contenido del artículo.", result);

        // Verifies that GetCleanArticleSync returns the cleaned article content.
    }
    [Fact]
    public async Task GetRawHtmlAsync_ReturnsRawHtml()
    {
        // ARRANGE

        var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Contenido original.</p>
                          </article>
                      </body>
                  </html>
                  """;

        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            rawHtml);

        var httpClient = new HttpClient(handler);

        var service = new ArticleReaderService();

        var httpClientField = typeof(ArticleReaderService)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);

        httpClientField!.SetValue(service, httpClient);

        // ACT

        var result = await service.GetRawHtmlAsync("https://example.com/article");

        // ASSERT

        Assert.Equal(rawHtml, result);

        // Verifies that GetRawHtmlAsync returns the original HTML without processing it.
    }
}
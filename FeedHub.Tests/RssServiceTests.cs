
using System.Net;


namespace FeedHub.Tests;

public class RssServiceTests
{
    
    [Fact]
    public async Task GetNewsAsync_ReturnsEmpty()
    {
        // ARRANGE

        var logger = new TestLogger();
        var http = new HttpClient();
        var feedUrl = String.Empty;
        var categoryFromDict = String.Empty;
        CancellationToken ct = default;

        var rssService = new RssService(logger, http);

        // ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        Assert.Empty(news);
    }
    [Fact]
    public async Task GetNewsAsync_ReturnsEmptyValueWhenResponseIsForbidden403()
    {
        //ARRANGE

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.Forbidden, String.Empty);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.urlfalsa.com/feed";
        var categoryFromDict = String.Empty;
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        //ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        //ASSERT

        Assert.Empty(news);

    }
    [Fact]
    public async Task GetNewsAsync_ThrowsHttpRequestExceptionWhenResponseGivesError500()
    {
        //ARRANGE

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError, String.Empty);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.urlfalsa.com/feed";
        var categoryFromDict = String.Empty;
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        //ACT / ASSERT

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
    () => rssService.GetNewsAsync(feedUrl, categoryFromDict, ct));
    }
    [Fact]
    public async Task GetNewsAsync_ReturnsNewsFromValidRss()
    {
        //ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia de prueba</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        //ACT 

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        Assert.Single(news);
    }
    
    [Fact]
    public async Task GetNewsAsync_MapsNewsItemPropertiesCorrectly()
    {
        // ARRANGE

        var rss = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
                <channel>
                    <title>Fuente de prueba</title>
                    <link>https://www.example.com</link>
                    <description>Feed de prueba</description>
                    <item>
                        <title>Noticia de prueba</title>
                        <link>https://www.example.com/noticia</link>
                        <description>Descripción de la noticia</description>
                        <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    </item>
                </channel>
            </rss>
            """;

                var logger = new TestLogger();
                var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
                var http = new HttpClient(handler);
                var feedUrl = "https://www.example.com/feed";
                var categoryFromDict = "tecnologia";
                CancellationToken ct = default;
                var rssService = new RssService(logger, http);

                // ACT

                var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

                // ASSERT

                var article = Assert.Single(news);

                Assert.Equal("Noticia de prueba", article.Title);
                Assert.Equal("https://www.example.com/noticia", article.Link);
                Assert.Equal("Descripción de la noticia", article.Description);
                Assert.Equal("tecnologia", article.Category);
                Assert.Equal("Fuente de prueba", article.Source);
    }
    [Fact]
    public async Task GetNewsAsync_ConvertsHttpLinksToHttps()
    {
        // ARRANGE

        var rss = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
                <channel>
                    <title>Fuente de prueba</title>
                    <link>https://www.example.com</link>
                    <description>Feed de prueba</description>
                    <item>
                        <title>Noticia de prueba</title>
                        <link>http://www.example.com/noticia</link>
                        <description>Descripción de la noticia</description>
                        <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    </item>
                </channel>
            </rss>
            """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        var article = Assert.Single(news);

        Assert.Equal("https://www.example.com/noticia", article.Link);
    }
    [Fact]
    public async Task GetNewsAsync_ThrowsOperationCanceledExceptionWhenCancellationIsRequested()
    {
        // ARRANGE

        var rss = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
                <channel>
                    <title>Fuente de prueba</title>
                    <link>https://www.example.com</link>
                    <description>Feed de prueba</description>
                    <item>
                        <title>Noticia de prueba</title>
                        <link>https://www.example.com/noticia</link>
                        <description>Descripción de la noticia</description>
                        <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    </item>
                </channel>
            </rss>
            """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var rssService = new RssService(logger, http);

        // ACT / ASSERT

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => rssService.GetNewsAsync(feedUrl, categoryFromDict, cts.Token));

    }
    [Fact]
    public async Task GetNewsAsync_UsesLastUpdatedTimeWhenPublishDateIsMissing()
    {
        // ARRANGE

        var rss = """
    <?xml version="1.0" encoding="UTF-8"?>
    <rss version="2.0"
        xmlns:atom="http://www.w3.org/2005/Atom">
        <channel>
            <title>Fuente de prueba</title>
            <link>https://www.example.com</link>
            <description>Feed de prueba</description>

            <item>
                <title>Noticia sin fecha de publicación</title>
                <link>https://www.example.com/noticia</link>
                <description>Descripción de la noticia</description>

                <!-- Used to test the fallback to LastUpdatedTime -->

                <atom:updated>2026-09-08T12:30:00Z</atom:updated>
            </item>
        </channel>
    </rss>
    """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        var article = Assert.Single(news);

        Assert.Equal(
            new DateTime(2026, 9, 8, 12, 30, 0),
            article.PublishDate);

    }
    [Fact]
    public async Task GetNewsAsync_RemovesHtmlFromTitle()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title><![CDATA[<b>Noticia importante</b>]]></title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);
        Assert.Equal("Noticia importante", article.Title);
    }
    [Fact]
    public async Task GetNewsAsync_DecodesHtmlEntities()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title><![CDATA[Apple &amp; Microsoft anuncian novedades]]></title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);
        Assert.Equal("Apple & Microsoft anuncian novedades", article.Title);
    }
    [Theory]
    [InlineData("Esta es una noticia. Leer más", "Esta es una noticia.")]
    [InlineData("Esta es una noticia. Ver más", "Esta es una noticia.")]
    [InlineData("Esta es una noticia. Sigue leyendo", "Esta es una noticia.")]
    public async Task GetNewsAsync_RemovesTrailingReadingPhrases(
    string description,
    string expectedDescription)
    {
        // ARRANGE
        var rss = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia de prueba</title>
                    <link>https://www.example.com/noticia</link>
                    <description>{description}</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);
        Assert.Equal(expectedDescription, article.Description);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromCustomExtension()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    <image>https://www.example.com/images/noticia.jpg</image>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);
        Assert.Equal(
            "https://www.example.com/images/noticia.jpg",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromEnclosure()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    <enclosure
                        url="https://www.example.com/images/noticia.png"
                        type="image/png" />
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);
        Assert.Equal(
            "https://www.example.com/images/noticia.png",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromDescriptionHtml()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description><![CDATA[
                        <p>Descripción de la noticia</p>
                        <img src="https://www.example.com/images/noticia.jpg" />
                    ]]></description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.jpg",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromDataSrc()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen lazy loading</title>
                    <link>https://www.example.com/noticia</link>
                    <description><![CDATA[
                        <p>Descripción de la noticia</p>
                        <img data-src="https://www.example.com/images/noticia.webp" />
                    ]]></description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.webp",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromContentExtension()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0"
             xmlns:media="http://search.yahoo.com/mrss/">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    <media:content
                        url="https://www.example.com/images/noticia.jpg"
                        type="image/jpeg" />
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.jpg",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromExtensionXmlFallback()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    <custom>
                        https://www.example.com/images/noticia.webp
                    </custom>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.webp",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_DoesNotExtractNonImageEnclosure()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia sin imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                    <enclosure
                        url="https://www.example.com/files/noticia.mp3"
                        type="audio/mpeg" />
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Null(article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsAvifImageFromDescriptionHtml()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia con imagen AVIF</title>
                    <link>https://www.example.com/noticia</link>
                    <description><![CDATA[
                        <p>Descripción de la noticia</p>
                        <img src="https://www.example.com/images/noticia.avif" />
                    ]]></description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.avif",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_FiltersHipertextualEntertainmentByBlacklistedTitle()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Hipertextual</title>
                <link>https://hipertextual.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Netflix anuncia una nueva serie</title>
                    <link>https://hipertextual.com/noticia/netflix-serie</link>
                    <description>Contenido de entretenimiento que no queremos en tecnología.</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://hipertextual.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        Assert.Empty(news);
    }
    [Fact]
    public async Task GetNewsAsync_FiltersHipertextualEntertainmentByRssCategory()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Hipertextual</title>
                <link>https://hipertextual.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Una nueva producción llega este otoño</title>
                    <link>https://hipertextual.com/noticia/nueva-produccion</link>
                    <description>Contenido de entretenimiento que no queremos en tecnología.</description>
                    <category>cine</category>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://hipertextual.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        Assert.Empty(news);
    }
    [Fact]
    public async Task GetNewsAsync_KeepsValidHipertextualTechnologyArticle()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Hipertextual</title>
                <link>https://hipertextual.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Nuevo avance en inteligencia artificial</title>
                    <link>https://hipertextual.com/noticia/nuevo-avance-ia</link>
                    <description>Una noticia sobre tecnología e inteligencia artificial.</description>
                    <category>tecnologia</category>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://hipertextual.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(
            "Nuevo avance en inteligencia artificial",
            article.Title);
    }
    [Fact]
    public async Task GetNewsAsync_FiltersHipertextualEntertainmentBySeriesCategory()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Hipertextual</title>
                <link>https://hipertextual.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Una producción llegará próximamente</title>
                    <link>https://hipertextual.com/noticia/nueva-serie</link>
                    <description>Contenido de entretenimiento.</description>
                    <category>series</category>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://hipertextual.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        Assert.Empty(news);
    }
    [Fact]
    public async Task GetNewsAsync_DoesNotApplyHipertextualFilterOutsideTechnology()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Hipertextual</title>
                <link>https://hipertextual.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Netflix anuncia una nueva serie</title>
                    <link>https://hipertextual.com/noticia/netflix-serie</link>
                    <description>Contenido de entretenimiento.</description>
                    <category>series</category>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://hipertextual.com/feed";
        var categoryFromDict = "entretenimiento";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal("Netflix anuncia una nueva serie", article.Title);
    }
    [Fact]
    public async Task GetNewsAsync_ReturnsMaximumOf20News()
    {
        // ARRANGE
        var items = string.Join(Environment.NewLine,
            Enumerable.Range(1, 25).Select(i => $"""
            <item>
                <title>Noticia {i}</title>
                <link>https://www.example.com/noticia-{i}</link>
                <description>Descripción de la noticia {i}</description>
                <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
            </item>
            """));

        var rss = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>

                {items}

            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        Assert.Equal(20, news.Count);
    }
    [Fact]
    public async Task GetNewsAsync_SkipsNewsWithoutValidLink()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia sin enlace</title>
                    <link></link>
                    <description>Esta noticia no tiene ningún enlace.</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        Assert.Empty(news);
    }
    [Fact]
    public async Task GetNewsAsync_UsesDefaultTitleWhenTitleIsMissing()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal("Sin título", article.Title);
    }
    [Fact]
    public async Task GetNewsAsync_UsesEmptyDescriptionWhenDescriptionIsMissing()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia sin descripción</title>
                    <link>https://www.example.com/noticia</link>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal(string.Empty, article.Description);
    }
    [Fact]
    public async Task GetNewsAsync_UsesGeneralCategoryWhenCategoryIsNull()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia de prueba</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        string? categoryFromDict = null;
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict!, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal("General", article.Category);
    }
    [Fact]
    public async Task GetNewsAsync_UsesFeedHostWhenSourceTitleIsMissing()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia de prueba</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Equal("www.example.com", article.Source);
    }
    [Fact]
    public async Task GetNewsAsync_ReturnsNullImageUrlWhenNoImageExists()
    {
        // ARRANGE
        var rss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
            <channel>
                <title>Fuente de prueba</title>
                <link>https://www.example.com</link>
                <description>Feed de prueba</description>
                <item>
                    <title>Noticia sin imagen</title>
                    <link>https://www.example.com/noticia</link>
                    <description>Descripción de la noticia sin imágenes.</description>
                    <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT
        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT
        var article = Assert.Single(news);

        Assert.Null(article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromEncodedContent()
    {
        // ARRANGE

        var rss = """
    <?xml version="1.0" encoding="UTF-8"?>
    <rss version="2.0"
         xmlns:content="http://purl.org/rss/1.0/modules/content/">
        <channel>
            <title>Fuente de prueba</title>
            <link>https://www.example.com</link>
            <description>Feed de prueba</description>
            <item>
                <title>Noticia con imagen en contenido codificado</title>
                <link>https://www.example.com/noticia</link>
                <description>Descripción de la noticia</description>
                <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                <content:encoded><![CDATA[
                    <p>Contenido completo de la noticia.</p>
                    <img src="https://www.example.com/images/noticia.jpg" />
                ]]></content:encoded>
            </item>
        </channel>
    </rss>
    """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.jpg",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ExtractsImageFromCustomExtensionAttribute()
    {
        // ARRANGE

        var rss = """
    <?xml version="1.0" encoding="UTF-8"?>
    <rss version="2.0">
        <channel>
            <title>Fuente de prueba</title>
            <link>https://www.example.com</link>
            <description>Feed de prueba</description>
            <item>
                <title>Noticia con imagen</title>
                <link>https://www.example.com/noticia</link>
                <description>Descripción de la noticia</description>
                <pubDate>Tue, 08 Sep 2026 10:00:00 GMT</pubDate>
                <image url="https://www.example.com/images/noticia.jpg" />
            </item>
        </channel>
    </rss>
    """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        var article = Assert.Single(news);

        Assert.Equal(
            "https://www.example.com/images/noticia.jpg",
            article.ImageUrl);
    }
    [Fact]
    public async Task GetNewsAsync_ThrowsExceptionWhenRssIsInvalid()
    {
        // ARRANGE

        var rss = """
    <?xml version="1.0" encoding="UTF-8"?>
    <rss version="2.0">
        <channel>
            <title>Fuente de prueba</title>
            <item>
                <title>Noticia incompleta</title>
        </channel>
    """;

        var logger = new TestLogger();
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, rss);
        var http = new HttpClient(handler);
        var feedUrl = "https://www.example.com/feed";
        var categoryFromDict = "tecnologia";
        CancellationToken ct = default;
        var rssService = new RssService(logger, http);

        // ACT / ASSERT

        await Assert.ThrowsAnyAsync<Exception>(
            () => rssService.GetNewsAsync(feedUrl, categoryFromDict, ct));
    }

// SUMMARY:
// These tests cover the main behavior of RssService, including RSS parsing,
// HTTP error handling, cancellation, news item mapping, date and link fallbacks,
// HTML cleaning, image extraction from different RSS formats, Hipertextual-specific
// filtering rules, missing data handling, and the maximum number of returned articles.
}
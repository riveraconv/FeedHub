
using System.Text;
using FeedHub_Core.Services;

namespace FeedHub.Tests;

public class QuickArticleResultServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Extract_ReturnsEmptyResultWhenHtmlIsEmpty(string? rawHtml)
    {
        //ARRANGE

        var service = new QuickArticleService();

        //ACT

        var result = await service.Extract(rawHtml!);

        //ASSERT

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Html);
        Assert.Equal(string.Empty, result.Title);
        Assert.Equal(string.Empty, result.ImageUrl);
        Assert.Equal(string.Empty, result.Text);

        // Verifies that Extract returns an empty result for null, empty, or whitespace HTML.
    }
    [Fact]
    public async Task Extract_ReturnsEmptyResultWhenArticleNodeIsNotFound()
    {
        // ARRANGE

        var service = new QuickArticleService();

        var rawHtml = """
                  <html>
                      <head>
                          <title>Test article</title>
                      </head>
                      <body>
                              <p>Contenido que no es un artículo</p>
                      </body>
                  </html>
                  """;

        // ACT

        var result = await service.Extract(rawHtml);

        // ASSERT

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Html);
        Assert.Equal(string.Empty, result.Title);
        Assert.Equal(string.Empty, result.ImageUrl);
        Assert.Equal(string.Empty, result.Text);

        // Verifies that Extract returns an empty result when no article node is found.
    }
    [Fact]
    public async Task Extract_UsesH1AsTitleWhenAvailable()
    {
        //ARRANGE

        var service = new QuickArticleService();
        var rawHtml = """
                  <html>
                      <body>
                          <h1>Mi título del artículo</h1>
                          <article>
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;
        //ACT

        var result = await service.Extract(rawHtml);

        //ASSERT

        Assert.NotNull(result);
        Assert.Equal("Mi título del artículo", result.Title);

        // Verifies that Extract uses the h1 element as the article title when available.
    }
    [Fact]
public async Task Extract_UsesTitleElementWhenH1IsNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <head>
                          <title>Mi título desde title</title>
                      </head>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("Mi título desde title", result.Title);

    // Verifies that Extract uses the title element when no h1 element is available.
}
[Fact]
public async Task Extract_UsesDefaultTitleWhenH1AndTitleAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("Sin título", result.Title);

    // Verifies that Extract uses the default title when neither h1 nor title is available.
}
[Fact]
public async Task Extract_UsesOpenGraphImageWhenAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <head>
                          <meta property="og:image" content="https://example.com/article-image.jpg" />
                      </head>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract uses a valid Open Graph image when available.
}
[Fact]
public async Task Extract_UsesArticleImageWhenOpenGraphImageIsNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <img src="https://example.com/article-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract uses the first valid article image when no Open Graph image is available.
}
[Fact]
public async Task Extract_UsesArticleImageFromDataSrcWhenAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <img data-src="https://example.com/lazy-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/lazy-image.jpg", result.ImageUrl);

    // Verifies that Extract uses the article image from data-src when available, lazy-loading.
}
[Fact]
public async Task Extract_IgnoresAvatarAndUsesNextValidArticleImage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <img src="https://example.com/avatar-author.jpg" />
                              <img src="https://example.com/article-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract ignores avatar images and uses the next valid article image.
}
[Fact]
public async Task Extract_IgnoresIconAndUsesNextValidArticleImage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <img src="https://example.com/share-icon.png" />
                              <img src="https://example.com/article-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract ignores icon images and uses the next valid article image.
}
[Fact]
public async Task Extract_IgnoresLogoAndUsesNextValidArticleImage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <img src="https://example.com/site-logo.png" />
                              <img src="https://example.com/article-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract ignores logo images and uses the next valid article image.
}
[Fact]
public async Task Extract_IgnoresInvalidOpenGraphImageAndUsesArticleImage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <head>
                          <meta property="og:image" content="https://example.com/image.svg" />
                      </head>
                      <body>
                          <article>
                              <img src="https://example.com/article-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract ignores an invalid Open Graph image and uses a valid article image instead.
}
[Fact]
public async Task Extract_IgnoresOpenGraphImageWithInvalidProtocolAndUsesArticleImage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <head>
                          <meta property="og:image" content="ftp://example.com/image.jpg" />
                      </head>
                      <body>
                          <article>
                              <img src="https://example.com/article-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract ignores an Open Graph image with an invalid protocol and uses a valid article image instead.
}
[Fact]
public async Task Extract_UsesProtocolRelativeOpenGraphImageWhenAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <head>
                          <meta property="og:image" content="//example.com/article-image.jpg" />
                      </head>
                      <body>
                          <article>
                              <img src="https://example.com/other-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("//example.com/article-image.jpg", result.ImageUrl);

    // Verifies that Extract accepts a protocol-relative Open Graph image URL.
}
[Fact]
public async Task Extract_UsesDataSrcBeforeSrcWhenBothAreAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <img src="https://example.com/src-image.jpg"
                                   data-src="https://example.com/data-src-image.jpg" />
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal("https://example.com/data-src-image.jpg", result.ImageUrl);

    // Verifies that Extract prefers data-src over src when both image attributes are available.
}
[Fact]
public async Task Extract_ReturnsEmptyImageUrlWhenNoArticleImageIsAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal(string.Empty, result.ImageUrl);

    // Verifies that Extract returns an empty image URL when no valid article image is available.
}
[Fact]
public async Task Extract_ReturnsEmptyTextWhenArticleContentIsEmpty()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article></article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Equal(string.Empty, result.Html);
    Assert.Equal(string.Empty, result.Text);

    // Verifies that Extract returns empty text when the article contains no content.
}
[Fact]
public async Task Extract_DecodesHtmlEntitiesInArticleText()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Noticias sobre econom&iacute;a &amp; tecnolog&iacute;a con suficiente contenido para superar el filtro.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("economía & tecnología", result.Text);

    // Verifies that Extract decodes HTML entities in the article text.
}
[Fact]
public async Task Extract_PreservesSeparateParagraphsAsSeparateLines()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el primer párrafo del artículo y contiene suficiente texto para superar el filtro.</p>
                              <p>Este es el segundo párrafo del artículo y también contiene suficiente texto para mantenerse.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("</p>\n<p>", result.Html);

    // Verifies that separate article paragraphs remain separated by new lines in the cleaned HTML.
}
[Fact]
public async Task Extract_ConvertsBrTagsToLineBreaks()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el primer texto del artículo con suficiente contenido para superar el filtro.<br>Este es el segundo texto después del salto de línea.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("\n", result.Html);
    Assert.Contains("Este es el primer texto", result.Html);
    Assert.Contains("Este es el segundo texto", result.Html);

    // Verifies that Extract converts br tags into line breaks in the cleaned HTML.
}
[Fact]
public async Task Extract_RemovesUnwantedHtmlTagsFromArticleText()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un texto suficientemente largo para superar el filtro de contenido.</p>
                              <script>contenido que no debe aparecer</script>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("<script>", result.Html);
    Assert.DoesNotContain("contenido que no debe aparecer", result.Html);

    // Verifies that Extract removes unwanted HTML tags and their content from the article text.
    // This test don't tottally isolates the regular expression from CleanText(), the behaviour is covered by
    // GetCleanArticleNode(). 
}
[Fact]
public async Task Extract_PreservesAllowedFormattingTags()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un texto suficientemente largo para superar el filtro de contenido.</p>
                              <p>Este texto contiene <strong>información importante</strong> que debe conservar su formato.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("<strong>información importante</strong>", result.Html);

    // Verifies that Extract preserves allowed formatting tags in the cleaned HTML.
}
[Fact]
public async Task Extract_RemovesUnallowedFormattingTagsButKeepsTheirText()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un texto suficientemente largo para superar el filtro de contenido.</p>
                              <p>Este texto contiene <u>formato no permitido</u> que debe conservar su contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("<u>", result.Html);
    Assert.Contains("formato no permitido", result.Html);

    // Verifies that Extract removes unallowed formatting tags while preserving their text content.
}
[Fact]
public async Task Extract_PreservesAllAllowedFormattingTags()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un texto suficientemente largo para superar el filtro de contenido.</p>
                              <p><b>Texto en negrita</b> y <i>texto en cursiva</i> y <em>texto enfatizado</em>.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("<b>Texto en negrita</b>", result.Html);
    Assert.Contains("<i>texto en cursiva</i>", result.Html);
    Assert.Contains("<em>texto enfatizado</em>", result.Html);

    // Verifies that Extract preserves all allowed formatting tags in the cleaned HTML.
}
[Fact]
public async Task Extract_IgnoresArticleLinesShorterThanMinimumLength()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Texto corto</p>
                              <p>Este es un párrafo suficientemente largo para superar el filtro de contenido.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("Texto corto", result.Html);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);

    // Verifies that Extract ignores article lines shorter than 25 characters.
}
[Fact]
public async Task Extract_IgnoresLinesContainingNoisePatterns()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Esta sección contiene publicidad y no forma parte del artículo.</p>
                              <p>Este es un párrafo suficientemente largo que sí debe formar parte del artículo.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("Esta sección contiene publicidad", result.Html);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);

    // Verifies that Extract ignores article lines containing configured noise patterns.
}
[Fact]
public async Task Extract_IgnoresLinesStartingWithHttp()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>https://example.com/some-link-that-should-not-appear-in-the-article</p>
                              <p>Este es un párrafo suficientemente largo que sí debe formar parte del artículo.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("https://example.com/some-link-that-should-not-appear", result.Html);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);

    // Verifies that Extract ignores article lines starting with http.
}
[Fact]
public async Task Extract_IgnoresLinesContainingPicTwitter()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>https://pic.twitter.com/example-image-link-that-should-not-appear</p>
                              <p>Este es un párrafo suficientemente largo que sí debe formar parte del artículo.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("pic.twitter.com/example-image-link", result.Html);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);

    // Verifies that Extract ignores article lines containing pic.twitter.
}
[Fact]
public async Task Extract_IgnoresLinesContainingEuroPrices()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>El producto tiene actualmente un precio de 19,99 € en esta tienda.</p>
                              <p>Este es un párrafo suficientemente largo que sí debe formar parte del artículo.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("19,99 €", result.Html);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);

    // Verifies that Extract ignores article lines containing prices in euros.
}
[Fact]
public async Task Extract_IgnoresLinesContainingEuroPricesWithEuros()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>El producto tiene actualmente un precio de 19.99 euros en esta tienda.</p>
                              <p>Este es un párrafo suficientemente largo que sí debe formar parte del artículo.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.DoesNotContain("19.99 euros", result.Html);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);

    // Verifies that Extract ignores article lines containing prices followed by euros.
}
[Fact]
public async Task Extract_NormalizesMultipleSpacesInArticleText()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este    texto     contiene     múltiples     espacios     consecutivos.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este texto contiene múltiples espacios consecutivos.", result.Html);
    Assert.DoesNotContain("Este    texto", result.Html);

    // Verifies that Extract normalizes consecutive whitespace into single spaces.
}
[Fact]
public async Task Extract_StopsProcessingWhenStopPatternIsFound()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo que debe formar parte del artículo.</p>
                              <p>Sigue leyendo para encontrar más información.</p>
                              <p>Este contenido aparece después del patrón de parada y no debe incluirse.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Sigue leyendo", result.Html);
    Assert.DoesNotContain("Este contenido aparece después del patrón de parada", result.Html);

    // Verifies that Extract stops processing article text when a stop pattern is found.
}
[Fact]
public async Task Extract_RemovesCopyrightResidualGarbage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo que debe formar parte del artículo.</p>
                              <p>Copyright 2026 Example News. Todos los derechos reservados.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Copyright 2026 Example News", result.Html);

    // Verifies that Extract removes residual copyright text from the cleaned article HTML.
}
[Fact]
public async Task Extract_RemovesRightsReservedResidualGarbage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo que debe formar parte del artículo.</p>
                              <p>Derechos reservados 2026 Example News.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Derechos reservados 2026 Example News", result.Html);

    // Verifies that Extract removes residual rights reserved text from the cleaned article HTML.
}
[Fact]
public async Task Extract_RemovesCopyrightSymbolResidualGarbage()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo que debe formar parte del artículo.</p>
                              <p>© 2026 Example News. Todos los derechos reservados.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("© 2026 Example News", result.Html);

    // Verifies that Extract removes residual copyright symbol text from the cleaned article HTML.
}
[Fact]
public async Task Extract_UsesBlobContainerAsArticleNodeWhenAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="blob-container">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract prioritizes the blob-container element as the article content.
}
[Fact]
public async Task Extract_UsesArticleBodyAsArticleNodeWhenBlobContainerIsNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div itemprop="articleBody">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the articleBody element when blob-container is not available.
}
[Fact]
public async Task Extract_UsesArticleTextAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="article-text">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the article-text element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesArticleContentAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="article-content">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the article-content element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesPostContentAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="post-content">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the post-content element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesMainContentAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div id="main-content">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the main-content element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesMainContentClassAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="main-content">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the main-content class when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesContentInnerAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="content-inner">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the content-inner element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesEpArticleBodyAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div class="ep-article-body">
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </div>

                          <article>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the ep-article-body element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesArticleElementAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </article>

                          <main>
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </main>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the article element when higher-priority selectors are not available.
}
[Fact]
public async Task Extract_UsesMainElementAsArticleNodeWhenPreviousSelectorsAreNotAvailable()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <main>
                              <p>Este es el contenido correcto del artículo y debe ser utilizado por el servicio.</p>
                          </main>

                          <div class="other-content">
                              <p>Este contenido pertenece a otro candidato y no debe ser utilizado.</p>
                          </div>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido correcto del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a otro candidato", result.Html);

    // Verifies that Extract uses the main element when higher-priority selectors are not available.
}

//FALLBACK FOR GetBestContentNode()

[Fact]
public async Task Extract_UsesContentNodeWithMostLongParagraphsAsFallback()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div>
                              <p>Este es el contenido del primer nodo y contiene suficiente texto para contar como un párrafo largo.</p>
                          </div>

                          <div>
                              <p>Este es el primer párrafo del segundo nodo y también contiene suficiente texto para contar como largo.</p>
                              <p>Este es el segundo párrafo del segundo nodo y también contiene suficiente texto para contar como largo.</p>
                          </div>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el primer párrafo del segundo nodo", result.Html);
    Assert.Contains("Este es el segundo párrafo del segundo nodo", result.Html);
    Assert.DoesNotContain("Este es el contenido del primer nodo", result.Html);

    // Verifies that Extract selects the fallback content node with the most long paragraphs.
}
[Fact]
public async Task Extract_PenalizesFallbackContentNodesWithHighLinkDensity()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div>
                              <p>
                                  <a href="https://example.com/1">Este es un enlace con mucho contenido de texto.</a>
                                  <a href="https://example.com/2">Este es otro enlace con mucho contenido de texto.</a>
                                  <a href="https://example.com/3">Y este es un tercer enlace con mucho contenido.</a>
                              </p>
                          </div>

                          <div>
                              <p>Este es el contenido real del artículo y contiene suficiente texto para ser considerado contenido válido.</p>
                          </div>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido real del artículo", result.Html);
    Assert.DoesNotContain("Este es un enlace con mucho contenido", result.Html);

    // Verifies that Extract penalizes fallback content nodes with high link density.
}
[Fact]
public async Task Extract_RemovesAsideFromArticleContent()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el contenido principal del artículo y debe mantenerse después de la limpieza.</p>
                              <aside>
                                  <p>Este contenido pertenece a una sección lateral y debe eliminarse.</p>
                              </aside>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido principal del artículo", result.Html);
    Assert.DoesNotContain("Este contenido pertenece a una sección lateral", result.Html);

    // Verifies that Extract removes aside elements from the selected article content.
}

//Selection for sensitive cases of selectors, not all cases were covered

[Fact]
public async Task Extract_RemovesRelatedContentFromArticle()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es el contenido principal del artículo y debe mantenerse después de la limpieza.</p>

                              <div class="related">
                                  <p>Esta es una noticia relacionada y debe eliminarse del contenido.</p>
                              </div>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido principal del artículo", result.Html);
    Assert.DoesNotContain("Esta es una noticia relacionada", result.Html);

    // Verifies that Extract removes related content from the selected article node.
}
[Fact]
public async Task Extract_RemovesShortLinkParagraphs()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo que debe mantenerse en el contenido final.</p>
                              <p><a href="https://example.com">Enlace corto</a></p>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Enlace corto", result.Html);

    // Verifies that Extract removes short paragraphs containing a single link.
}
[Fact]
public async Task Extract_KeepsLongParagraphsWhenArticleHasHighLinkDensity()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p><a href="https://example.com/1">Enlace corto</a></p>
                              <p>Este es un párrafo suficientemente largo que debe conservarse porque contiene más de cuarenta caracteres.</p>
                              <p>Otro párrafo suficientemente largo que también debe mantenerse dentro del contenido limpio.</p>
                              <a href="https://example.com/2">Enlace adicional</a>
                              <a href="https://example.com/3">Otro enlace adicional</a>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.Contains("Otro párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Enlace corto", result.Html);

    // Verifies that high link density keeps only sufficiently long paragraphs.
    //This test checks the behaviour for the selected node when is clean already. Don't verifies per lenght, it was already
    //managed by past tests.
}
[Fact]
public async Task Extract_HandlesHighLinkDensityWithoutParagraphs()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <a href="https://example.com/1">Este es un enlace suficientemente largo para formar parte del contenido.</a>
                              <a href="https://example.com/2">Este es otro enlace suficientemente largo para aumentar la densidad de enlaces.</a>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un enlace suficientemente largo", result.Html);
    Assert.Contains("Este es otro enlace suficientemente largo", result.Html);

    // Verifies that Extract handles high link density when no paragraphs are present.
}
[Fact]
public async Task Extract_IgnoresFallbackContentNodesWithoutParagraphs()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <div>
                              <div>
                                  <span>Este contenido no está dentro de un párrafo y no debe ser seleccionado como contenido principal.</span>
                              </div>

                              <div>
                                  <p>Este es el contenido principal del artículo y debe ser seleccionado por el algoritmo de fallback.</p>
                                  <p>Este es otro párrafo suficientemente largo que confirma que el nodo correcto ha sido seleccionado.</p>
                              </div>
                          </div>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es el contenido principal del artículo", result.Html);
    Assert.Contains("Este es otro párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Este contenido no está dentro de un párrafo", result.Html);

    // Verifies that fallback content nodes without paragraphs receive no score.
}
[Fact]
public async Task Extract_RemovesLongParagraphsContainingStopPatternsWhenArticleHasHighLinkDensity()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  <html>
                      <body>
                          <article>
                              <p>Este es un párrafo suficientemente largo que debe conservarse dentro del contenido final.</p>
                              <p>Sigue leyendo para descubrir toda la información y los detalles adicionales de esta noticia tan importante.</p>
                              <a href="https://example.com/1">Enlace adicional para aumentar la densidad de enlaces.</a>
                              <a href="https://example.com/2">Otro enlace adicional para aumentar la densidad de enlaces.</a>
                              <a href="https://example.com/3">Un tercer enlace adicional para aumentar la densidad de enlaces.</a>
                          </article>
                      </body>
                  </html>
                  """;

    // ACT

    var result = await service.Extract(rawHtml);

    // ASSERT

    Assert.NotNull(result);
    Assert.Contains("Este es un párrafo suficientemente largo", result.Html);
    Assert.DoesNotContain("Sigue leyendo para descubrir toda la información", result.Html);

    // Verifies that high-link-density content removes long paragraphs containing stop patterns.
}

[Fact]
public void DecodeHtml_UsesProvidedCharsetWhenValid()
{
    // ARRANGE

    var service = new QuickArticleService();

    var expectedHtml = "<p>Noticias de España: año, información y economía.</p>";
    var bytes = Encoding.UTF8.GetBytes(expectedHtml);

    // ACT

    var result = service.DecodeHtml(bytes, "utf-8");

    // ASSERT

    Assert.Equal(expectedHtml, result);

    // Verifies that DecodeHtml uses the provided charset when it is valid.
}
[Fact]
public void DecodeHtml_UsesUtf8WhenCharsetIsNull()
{
    // ARRANGE

    var service = new QuickArticleService();

    var expectedHtml = "<p>Noticias de España: año, información y economía.</p>";
    var bytes = Encoding.UTF8.GetBytes(expectedHtml);

    // ACT

    var result = service.DecodeHtml(bytes, null);

    // ASSERT

    Assert.Equal(expectedHtml, result);

    // Verifies that DecodeHtml uses UTF-8 when no charset is provided.
}
[Fact]
public void DecodeHtml_UsesUtf8WhenCharsetIsEmpty()
{
    // ARRANGE

    var service = new QuickArticleService();

    var expectedHtml = "<p>Noticias de España: año, información y economía.</p>";
    var bytes = Encoding.UTF8.GetBytes(expectedHtml);

    // ACT

    var result = service.DecodeHtml(bytes, "");

    // ASSERT

    Assert.Equal(expectedHtml, result);

    // Verifies that DecodeHtml uses UTF-8 when the charset is empty.
}
[Fact]
public void DecodeHtml_UsesUtf8WhenCharsetIsInvalid()
{
    // ARRANGE

    var service = new QuickArticleService();

    var expectedHtml = "<p>Noticias de España: año, información y economía.</p>";
    var bytes = Encoding.UTF8.GetBytes(expectedHtml);

    // ACT

    var result = service.DecodeHtml(bytes, "invalid-charset");

    // ASSERT

    Assert.Equal(expectedHtml, result);

    // Verifies that DecodeHtml falls back to UTF-8 when the charset is invalid.
}
[Fact]
public void DecodeHtml_NormalizesWhitespaceAndTrimsResult()
{
    // ARRANGE

    var service = new QuickArticleService();

    var rawHtml = """
                  
                      <p>Este    texto
                      contiene    varios espacios y saltos de línea.</p>
                  
                  """;

    var bytes = Encoding.UTF8.GetBytes(rawHtml);

    // ACT

    var result = service.DecodeHtml(bytes, "utf-8");

    // ASSERT

    Assert.Equal("<p>Este texto contiene varios espacios y saltos de línea.</p>", result);

    // Verifies that DecodeHtml normalizes whitespace and trims the result.
}
}
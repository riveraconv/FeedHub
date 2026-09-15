using FeedHub_Core.Services;

namespace FeedHub.Tests;

public class SourceCatalogTests
{
    [Fact]
    public void GetSources_ReadsAllExpectedSourcesAndFeeds()
    {
        //ARRANGE

        var catalog = new SourceCatalogService();

        //ACT

        var sources = catalog.GetSources();

        //ASSERT

        Assert.Equal(21, sources.Count);
        Assert.Equal(64, sources.SelectMany(source => source.Feeds).Count());

        var elPais = Assert.Single(sources.Where(source => source.Id == "elpais"));
        Assert.Equal(9, elPais.Feeds.Count);
    }
    [Fact]
    public void GetSourceById_ReturnsExpectedSource()
    {
        //ARRANGE

        var catalog = new SourceCatalogService();

        //ACT

        var source = catalog.GetSourceById("elpais");

        //ASSERT

        Assert.NotNull(source);
        Assert.Equal("elpais", source.Id);
    }
    [Fact]
    public void GetSourceById_IsCaseInsensitive()
    {
        //ARRANGE

        var catalog = new SourceCatalogService();

        //ACT

        var source = catalog.GetSourceById("ElPaIs");

        //ASSERT

        Assert.NotNull(source);
        Assert.Equal("elpais", source.Id);
    }

    [Fact]
    public void GetSourceById_ReturnsNullForUnknownSource()
    {
        //ARRANGE

        var catalog = new SourceCatalogService();

        //ACT

        var source = catalog.GetSourceById("fuente-que-no-existe");

        //ASSERT

        Assert.Null(source);
    }

    [Fact]
    public void GetSourceName_ReturnsExpectedName()
    {
        //ARRANGE

        var catalog = new SourceCatalogService();

        //ACT

        var name = catalog.GetSourceName("elpais");

        //ASSERT

        Assert.Equal("El País", name);
    }

    [Fact]
    public void GetSourceName_ReturnsDefaultForUnknownSource()
    {
        //ARRANGE

        var catalog = new SourceCatalogService();

        //ACT

        var name = catalog.GetSourceName("fuente-que-no-existe");

        //ASSERT

        Assert.Equal("Fuente desconocida", name);
    }

    /*
    * Test coverage summary:
    * - Verifies that all expected sources and feeds are loaded.
    * - Verifies source retrieval by ID.
    * - Verifies case-insensitive source ID lookup.
    * - Verifies null is returned for unknown sources.
    * - Verifies the correct display name is returned for known sources.
    * - Verifies the default name is returned for unknown sources.
    */
}

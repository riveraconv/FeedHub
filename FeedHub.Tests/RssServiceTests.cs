using FeedHub_Core.Services;

namespace FeedHub.Tests;

public class SourceCatalogTests
{
    [Fact]
    public void GetSources_ReadsAllExpectedSourcesAndFeeds()
    {
        var catalog = new SourceCatalogService();

        var sources = catalog.GetSources();

        Assert.Equal(21, sources.Count);
        Assert.Equal(64, sources.SelectMany(source => source.Feeds).Count());

        var elPais = Assert.Single(sources.Where(source => source.Id == "elpais"));
        Assert.Equal(9, elPais.Feeds.Count);
    }
}

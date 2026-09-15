using FeedHub_App.ViewModels.News;
using FeedHub_Core.Services;

namespace FeedHub.App.Tests;

public class CategoriesBySourceViewModelTests
{
    [Fact]
    public void SourceGroup_SetsCategoryTitleAndContainsSources()
    {
        // ARRANGE
        var sources = new[]
        {
            new NewsSource
            {
                Id = "source1",
                Name = "Source 1"
            },
            new NewsSource
            {
                Id = "source2",
                Name = "Source 2"
            }
        };

        // ACT
        var group = new SourceGroup("Technology", sources);

        // ASSERT
        Assert.Equal("Technology", group.CategoryTitle);
        Assert.Equal(2, group.Count);
        Assert.Same(sources[0], group[0]);
        Assert.Same(sources[1], group[1]);
        // Verifies that SourceGroup stores its category title and supplied sources.
    }
    [Fact]
    public void Constructor_ConvertsSourceNamesToUpperCase()
    {
        // ARRANGE
        var sourceCatalog = new SourceCatalogService();

        // ACT
        var viewModel = new CategoriesBySourceViewModel(sourceCatalog);

        // ASSERT
        Assert.NotEmpty(viewModel.GroupedSources);

        Assert.All(
            viewModel.GroupedSources.SelectMany(group => group),
            source => Assert.Equal(source.Name, source.Name.ToUpperInvariant()));

        // Verifies that all source names are converted to uppercase.
    }
    [Fact]
    public void Constructor_GroupsSourcesByCategory()
    {
        // ARRANGE
        var sourceCatalog = new SourceCatalogService();

        // ACT
        var viewModel = new CategoriesBySourceViewModel(sourceCatalog);

        // ASSERT
        Assert.NotEmpty(viewModel.GroupedSources);

        Assert.All(
            viewModel.GroupedSources,
            group =>
            {
                Assert.False(string.IsNullOrEmpty(group.CategoryTitle));
                Assert.NotEmpty(group);

                Assert.All(
                    group,
                    source => Assert.Equal(group.CategoryTitle, source.Category));
            });

        // Verifies that sources are grouped under the correct category.
    }
    [Fact]
    public void Constructor_PreservesSourceProperties()
    {
        // ARRANGE
        var sourceCatalog = new SourceCatalogService();
        var originalSources = sourceCatalog.GetSources();

        // ACT
        var viewModel = new CategoriesBySourceViewModel(sourceCatalog);
        var groupedSources = viewModel.GroupedSources.SelectMany(group => group).ToList();

        // ASSERT
        Assert.Equal(originalSources.Count, groupedSources.Count);

        foreach (var original in originalSources)
        {
            var result = Assert.Single(groupedSources.Where(source => source.Id == original.Id));

            Assert.Equal(original.Description, result.Description);
            Assert.Equal(original.Domain, result.Domain);
        }

        // Verifies that source identifiers, descriptions, and domains are preserved.
    }
}
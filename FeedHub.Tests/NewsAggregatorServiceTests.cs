using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Services;


namespace FeedHub.Tests;

public class NewsAggregatorServiceTests()
{
    [Fact]
    public void GetAvailableCategories_ReturnsDistinctCategoriesInAlphabeticalOrder()
    {
        // ARRANGE

        var rssService = new TestRssService();
        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var sourceCatalog = new SourceCatalogService();

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT

        var result = service.GetAvailableCategories();

        // ASSERT

        Assert.Equal(9, result.Count);
        Assert.Equal(result.OrderBy(category => category), result);
        Assert.Equal(result.Count, result.Distinct().Count());

        // Verifies that available categories are unique, complete, and alphabetically ordered.
    }
    [Fact]
    public void GetAvailableSources_ReturnsDistinctSourcesInAlphabeticalOrder()
    {
        // ARRANGE

        var rssService = new TestRssService();
        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var sourceCatalog = new SourceCatalogService();

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT

        var result = service.GetAvailableSources();

        // ASSERT

        Assert.Equal(21, result.Count);
        Assert.Equal(result.OrderBy(source => source), result);
        Assert.Equal(result.Count, result.Distinct().Count());

        // Verifies that available sources are unique, complete, and alphabetically ordered.
    }
    [Fact]
    public async Task GetLatestMixedAsync_ReturnsFilteredOutWhenAllSourcesAreDisabled()
    {
        // ARRANGE

        var rssService = new TestRssService();
        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var sourceCatalog = new SourceCatalogService();

        var disabledSources = sourceCatalog
            .GetSources()
            .Select(source => source.Id)
            .ToHashSet();

        preferences.Set(
            "disabled_sources",
            System.Text.Json.JsonSerializer.Serialize(disabledSources));

        var filterService = new FilterPreferencesService(preferences);

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT

        var result = await service.GetLatestMixedAsync(10);

        // ASSERT

        Assert.Equal(NewsQueryStatus.FilteredOut, result.Status);
        Assert.Empty(result.Items);

        // Verifies that Latest News returns FilteredOut when all sources are disabled.
    }
    [Fact]
    public async Task GetLatestMixedAsync_ReturnsNoContentWhenActiveSourcesHaveNoActiveCategories()
    {
        // ARRANGE

        var rssService = new TestRssService();
        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var sourceCatalog = new SourceCatalogService();

        var activeSource = sourceCatalog.GetSources().First();

        var disabledSources = sourceCatalog
            .GetSources()
            .Skip(1)
            .Select(source => source.Id)
            .ToHashSet();

        var disabledCategories = activeSource.Feeds
            .Select(feed => feed.Category)
            .ToHashSet();

        preferences.Set(
            "disabled_sources",
            System.Text.Json.JsonSerializer.Serialize(disabledSources));

        preferences.Set(
            "disabled_categories",
            System.Text.Json.JsonSerializer.Serialize(disabledCategories));

        var filterService = new FilterPreferencesService(preferences);

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT

        var result = await service.GetLatestMixedAsync(10);

        // ASSERT

        Assert.Equal(NewsQueryStatus.NoContent, result.Status);
        Assert.Empty(result.Items);

        // Verifies that Latest News returns NoContent when active sources have no active categories.
    }
    [Fact]
    public async Task GetLatestMixedAsync_ReturnsSuccessWithNewsFromActiveFeeds()
    {
        // ARRANGE

        var rssService = new TestRssService
        {
            Items = new List<NewsItem>
        {
            new NewsItem
            {
                Title = "Older News",
                PublishDate = DateTime.Now.AddMinutes(-10)
            },
            new NewsItem
            {
                Title = "Newer News",
                PublishDate = DateTime.Now.AddMinutes(-5)
            }
        }
        };

        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var sourceCatalog = new SourceCatalogService();

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT

        var result = await service.GetLatestMixedAsync(10);

        // ASSERT

        Assert.Equal(NewsQueryStatus.Success, result.Status);
        Assert.NotEmpty(result.Items);
        Assert.Equal("Newer News", result.Items[0].Title);

        for (int i = 1; i < result.Items.Count; i++)
        {
            Assert.True(
                result.Items[i - 1].PublishDate >= result.Items[i].PublishDate);
        }

        Assert.All(result.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.Source)));
        Assert.All(result.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.Category)));

        // Verifies that Latest News returns successful, sorted items with source and category assigned.
    }

    [Fact]
    public async Task GetLatestMixedAsync_ThrowsWhenAllFeedsFail()
    {
        // ARRANGE

        var rssService = new TestRssService
        {
            ExceptionToThrow = new HttpRequestException("Test exception")
        };

        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var sourceCatalog = new SourceCatalogService();

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT & ASSERT

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetLatestMixedAsync(10));

        // Verifies that Latest News throws when all active RSS feeds fail.
    }
    [Fact]
    public async Task GetLatestMixedAsync_ReturnsSuccessWhenSomeFeedsFail()
    {
        // ARRANGE

        var sourceCatalog = new SourceCatalogService();
        var feeds = sourceCatalog
            .GetSources()
            .SelectMany(source => source.Feeds)
            .ToList();

        var rssService = new TestRssService
        {
            Items =
            [
                new NewsItem
            {
                Title = "Test News",
                PublishDate = DateTime.Now
            }
            ],
            FailingFeedUrls =
            [
                feeds[0].Url
            ]
        };

        var logger = new TestLogger();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);

        var service = new NewsAggregatorService(
            rssService,
            logger,
            filterService,
            sourceCatalog);

        // ACT

        var result = await service.GetLatestMixedAsync(10);

        // ASSERT

        Assert.Equal(NewsQueryStatus.Success, result.Status);
        Assert.NotEmpty(result.Items);
        Assert.All(
            result.Items,
            item => Assert.Equal("Test News", item.Title));

        // Verifies that Latest News succeeds when at least one active RSS feed fails.
    }
    [Fact]
public async Task GetLatestMixedAsync_ReturnsNoContentWhenFeedsHaveNoNews()
{
    // ARRANGE

    var rssService = new TestRssService
    {
        Items = new List<NewsItem>()
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetLatestMixedAsync(10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.NoContent, result.Status);
    Assert.Empty(result.Items);

    // Verifies that Latest News returns NoContent when active feeds provide no news.
}
[Fact]
public async Task GetByCategoryAsync_ReturnsNoFeedsConfiguredWhenCategoryDoesNotExist()
{
    // ARRANGE

    var rssService = new TestRssService();
    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetByCategoryAsync("CategoriaInexistente", 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.NoFeedsConfigured, result.Status);
    Assert.Empty(result.Items);

    // Verifies that an unknown category returns NoFeedsConfigured without querying RSS feeds.
}
[Fact]
public async Task GetByCategoryAsync_ReturnsFilteredOutWhenAllCategorySourcesAreDisabled()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();
    var category = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .Select(feed => feed.Category)
        .First();

    var categorySourceIds = sourceCatalog
        .GetSources()
        .Where(source => source.Feeds.Any(feed =>
            string.Equals(feed.Category, category, StringComparison.OrdinalIgnoreCase)))
        .Select(source => source.Id)
        .ToHashSet();

    var preferences = new TestPreferencesService();

    preferences.Set(
        "disabled_sources",
        System.Text.Json.JsonSerializer.Serialize(categorySourceIds));

    var rssService = new TestRssService();
    var logger = new TestLogger();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetByCategoryAsync(category, 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.FilteredOut, result.Status);
    Assert.Empty(result.Items);

    // Verifies that a category returns FilteredOut when all its sources are disabled.
}
[Fact]
public async Task GetByCategoryAsync_ReturnsSuccessWithRecentNews()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var category = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .Select(feed => feed.Category)
        .First(category =>
            !string.Equals(
                category,
                "ciencia",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                category,
                "cultura",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                category,
                "entretenimiento",
                StringComparison.OrdinalIgnoreCase));

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Recent News",
                PublishDate = DateTime.Now.AddDays(-1)
            },
            new NewsItem
            {
                Title = "Old News",
                PublishDate = DateTime.Now.AddDays(-8)
            }
        ]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetByCategoryAsync(category, 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.Success, result.Status);
    Assert.NotEmpty(result.Items);
    Assert.All(
        result.Items,
        item => Assert.Equal("Recent News", item.Title));
    Assert.All(
        result.Items,
        item => Assert.Equal(category, item.Category));
    Assert.All(
        result.Items,
        item => Assert.False(string.IsNullOrWhiteSpace(item.Source)));

    // Verifies that category news excludes outdated items and assigns category and source correctly.
}
[Theory]
[InlineData("ciencia")]
[InlineData("cultura")]
[InlineData("entretenimiento")]
public async Task GetByCategoryAsync_UsesTwoDayCutoffForShortTermCategories(
    string category)
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Recent News",
                PublishDate = DateTime.Now.AddDays(-1)
            },
            new NewsItem
            {
                Title = "Old News",
                PublishDate = DateTime.Now.AddDays(-3)
            }
        ]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetByCategoryAsync(category, 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.Success, result.Status);
    Assert.NotEmpty(result.Items);
    Assert.All(
        result.Items,
        item => Assert.Equal("Recent News", item.Title));
    Assert.All(
    result.Items,
    item => Assert.Equal(
        category,
        item.Category,
        ignoreCase: true));

    // Verifies that short-term categories use a two-day cutoff and exclude older news.
}
[Fact]
public async Task GetByCategoryAsync_ThrowsWhenAllFeedsFail()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var category = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .Select(feed => feed.Category)
        .First();

    var rssService = new TestRssService
    {
        ExceptionToThrow = new HttpRequestException("Test exception")
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT & ASSERT

    await Assert.ThrowsAsync<HttpRequestException>(
        () => service.GetByCategoryAsync(category, 10));

    // Verifies that category loading throws when all RSS feeds fail.
}
[Fact]
public async Task GetByCategoryAsync_ReturnsNoContentWhenNoRecentNewsIsAvailable()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var category = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .Select(feed => feed.Category)
        .First();

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Old News",
                PublishDate = DateTime.Now.AddDays(-8)
            }
        ]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetByCategoryAsync(category, 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.NoContent, result.Status);
    Assert.Empty(result.Items);

    // Verifies that category loading returns no content when all available news are outdated.
}
[Fact]
public async Task GetByCategoryAsync_ReturnsSuccessWhenSomeFeedsFail()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var category = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .Select(feed => feed.Category)
        .First();

    var categoryFeeds = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .Where(feed => string.Equals(
            feed.Category,
            category,
            StringComparison.OrdinalIgnoreCase))
        .ToList();

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Test News",
                PublishDate = DateTime.Now.AddDays(-1)
            }
        ],
        FailingFeedUrls = [categoryFeeds[0].Url]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetByCategoryAsync(category, 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.Success, result.Status);
    Assert.NotEmpty(result.Items);
    Assert.All(
        result.Items,
        item => Assert.Equal("Test News", item.Title));

    // Verifies that category loading succeeds when some RSS feeds fail but others remain available.
}
[Fact]
public async Task SearchByKeywordAsync_ReturnsEmptyWhenQueryIsEmpty()
{
    // ARRANGE

    var rssService = new TestRssService();
    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.SearchByKeywordAsync(string.Empty, 10);

    // ASSERT

    Assert.Empty(result);

    // Verifies that keyword search returns no results when the query is empty.
}
[Fact]
public async Task SearchByKeywordAsync_ReturnsItemsMatchingTitle()
{
    // ARRANGE

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Breaking Technology News",
                Description = "Some description",
                PublishDate = DateTime.Now.AddMinutes(-5)
            },
            new NewsItem
            {
                Title = "Sports News",
                Description = "Another description",
                PublishDate = DateTime.Now.AddMinutes(-10)
            }
        ]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.SearchByKeywordAsync("technology", 10);

    // ASSERT

    Assert.NotEmpty(result);
    Assert.All(
        result,
        item => Assert.Contains(
            "technology",
            item.Title,
            StringComparison.OrdinalIgnoreCase));

    Assert.All(
        result,
        item => Assert.False(string.IsNullOrWhiteSpace(item.Source)));

    Assert.All(
        result,
        item => Assert.False(string.IsNullOrWhiteSpace(item.Category)));

    // Verifies that keyword search finds news whose title contains the query.
}
[Fact]
public async Task SearchByKeywordAsync_ReturnsItemsMatchingDescription()
{
    // ARRANGE

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "General News",
                Description = "Important technology development",
                PublishDate = DateTime.Now.AddMinutes(-5)
            },
            new NewsItem
            {
                Title = "Sports News",
                Description = "Football match results",
                PublishDate = DateTime.Now.AddMinutes(-10)
            }
        ]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.SearchByKeywordAsync("TECHNOLOGY", 10);

    // ASSERT

    Assert.NotEmpty(result);
    Assert.All(
        result,
        item => Assert.Contains(
            "technology",
            item.Description,
            StringComparison.OrdinalIgnoreCase));

    // Verifies that keyword search finds news whose description contains the query.
}
[Fact]
public async Task SearchByKeywordAsync_ReturnsEmptyWhenNoItemsMatch()
{
    // ARRANGE

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Technology News",
                Description = "Latest technology updates",
                PublishDate = DateTime.Now.AddMinutes(-5)
            }
        ]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.SearchByKeywordAsync("politics", 10);

    // ASSERT

    Assert.Empty(result);

    // Verifies that keyword search returns no results when no news matches the query.
}
[Fact]
public async Task SearchByKeywordAsync_ReturnsResultsOrderedByDateAndRespectsLimit()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var catalogFeeds = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .ToList();

    var workingFeedUrl = catalogFeeds[0].Url;

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Technology News 1",
                PublishDate = DateTime.Now.AddMinutes(-30)
            },
            new NewsItem
            {
                Title = "Technology News 2",
                PublishDate = DateTime.Now.AddMinutes(-10)
            },
            new NewsItem
            {
                Title = "Technology News 3",
                PublishDate = DateTime.Now.AddMinutes(-20)
            }
        ],
        FailingFeedUrls = catalogFeeds
            .Skip(1)
            .Select(feed => feed.Url)
            .ToHashSet()
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = (await service.SearchByKeywordAsync("technology", 2)).ToList();

    // ASSERT

    Assert.Equal(2, result.Count);
    Assert.Equal("Technology News 2", result[0].Title);
    Assert.Equal("Technology News 3", result[1].Title);

    Assert.True(
        result[0].PublishDate >= result[1].PublishDate);

    // Verifies that keyword search orders results by date and respects the requested limit.
}
[Fact]
public async Task SearchByKeywordAsync_ReturnsResultsWhenSomeFeedsFail()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();

    var catalogFeeds = sourceCatalog
        .GetSources()
        .SelectMany(source => source.Feeds)
        .ToList();

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Technology News",
                Description = "Latest technology update",
                PublishDate = DateTime.Now.AddMinutes(-5)
            }
        ],
        FailingFeedUrls = [catalogFeeds[0].Url]
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.SearchByKeywordAsync("technology", 10);

    // ASSERT

    Assert.NotEmpty(result);
    Assert.All(
        result,
        item => Assert.Equal("Technology News", item.Title));

    // Verifies that keyword search continues returning results when some RSS feeds fail.
}
[Fact]
public async Task SearchByKeywordAsync_ThrowsWhenAllFeedsFail()
{
    // ARRANGE

    var rssService = new TestRssService
    {
        ExceptionToThrow = new HttpRequestException("Test exception")
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT & ASSERT

    await Assert.ThrowsAsync<HttpRequestException>(
        () => service.SearchByKeywordAsync("technology", 10));

    // Verifies that keyword search throws when all RSS feeds fail.
}
[Fact]
public async Task GetBySourceAsync_ThrowsWhenSourceDoesNotExist()
{
    // ARRANGE

    var rssService = new TestRssService();
    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);
    var sourceCatalog = new SourceCatalogService();

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT & ASSERT

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.GetBySourceAsync("nonexistent-source", 10));

    // Verifies that requesting an unknown source throws an ArgumentException.
}
[Fact]
public async Task GetBySourceAsync_ReturnsNoContentWhenAllSourceCategoriesAreDisabled()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();
    var source = sourceCatalog.GetSources().First();

    var preferences = new TestPreferencesService();

    var disabledCategories = source.Feeds
        .Select(feed => feed.Category)
        .Distinct()
        .ToHashSet();

    preferences.Set(
        "disabled_categories",
        System.Text.Json.JsonSerializer.Serialize(disabledCategories));

    var rssService = new TestRssService();
    var logger = new TestLogger();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetBySourceAsync(source.Id, 10);

    // ASSERT

    Assert.Equal(NewsQueryStatus.NoContent, result.Status);
    Assert.Empty(result.Items);

    // Verifies that a source returns no content when all its categories are filtered out.
}
[Fact]
public async Task GetBySourceAsync_ReturnsSuccessWithOrderedLimitedNews()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();
    var source = sourceCatalog.GetSources().First();

    var sourceFeeds = source.Feeds.ToList();

    var rssService = new TestRssService
    {
        Items =
        [
            new NewsItem
            {
                Title = "Older News",
                PublishDate = DateTime.Now.AddMinutes(-30)
            },
            new NewsItem
            {
                Title = "Newer News",
                PublishDate = DateTime.Now.AddMinutes(-10)
            },
            new NewsItem
            {
                Title = "Middle News",
                PublishDate = DateTime.Now.AddMinutes(-20)
            }
        ],
        FailingFeedUrls = sourceFeeds
            .Skip(1)
            .Select(feed => feed.Url)
            .ToHashSet()
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT

    var result = await service.GetBySourceAsync(source.Id, 2);

    // ASSERT

    Assert.Equal(NewsQueryStatus.Success, result.Status);
    Assert.Equal(2, result.Items.Count);

    Assert.Equal("Newer News", result.Items[0].Title);
    Assert.Equal("Middle News", result.Items[1].Title);

    Assert.True(
        result.Items[0].PublishDate >= result.Items[1].PublishDate);

    Assert.All(
        result.Items,
        item => Assert.Equal(source.Name, item.Source));

    Assert.All(
        result.Items,
        item => Assert.False(string.IsNullOrWhiteSpace(item.Category)));

    // Verifies that source news are ordered by date, limited, and assigned to the correct source and category.
}
[Fact]
public async Task GetBySourceAsync_ThrowsWhenAllSourceFeedsFail()
{
    // ARRANGE

    var sourceCatalog = new SourceCatalogService();
    var source = sourceCatalog.GetSources().First();

    var rssService = new TestRssService
    {
        ExceptionToThrow = new HttpRequestException("Test exception")
    };

    var logger = new TestLogger();
    var preferences = new TestPreferencesService();
    var filterService = new FilterPreferencesService(preferences);

    var service = new NewsAggregatorService(
        rssService,
        logger,
        filterService,
        sourceCatalog);

    // ACT & ASSERT

    await Assert.ThrowsAsync<HttpRequestException>(
        () => service.GetBySourceAsync(source.Id, 10));

    // Verifies that source loading throws when all RSS feeds fail.
}
}
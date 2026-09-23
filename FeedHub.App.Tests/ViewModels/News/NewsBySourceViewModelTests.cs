using FeedHub.App.Tests.Helpers;
using FeedHub_Core.Services;
using FeedHub_Core.Models;
using FeedHub_App.ViewModels.News;



namespace FeedHub.App.Tests.ViewModels.News;

public class NewsBySourceViewModelTests
{
    [Fact]
    public void Constructor_InitializesExpectedState()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        // ACT
        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        // ASSERT
        Assert.Null(viewModel.SourceId);
        Assert.Null(viewModel.SourceTitle);
        Assert.False(viewModel.IsRefreshing);
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.NoResultsFound);
        Assert.False(viewModel.IsLoadingMore);
        Assert.False(viewModel.CanLoadMore);
        Assert.False(viewModel.IsContentEmpty);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
        Assert.Empty(viewModel.NewsItems);

        // Verifies that the ViewModel initializes all state properties with their expected default values.
    }
    [Fact]
    public void SourceId_WhenChanged_UpdatesSourceTitle()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        // ACT
        viewModel.SourceId = "elpais";

        // ASSERT
        Assert.Equal("El País", viewModel.SourceTitle);

        // Verifies that changing SourceId resolves and updates the corresponding source title.
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SourceId_WhenEmptyOrNull_DoesNotUpdateSourceTitle(string? sourceId)
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        // ACT
        viewModel.SourceTitle = "Título anterior";
        viewModel.SourceId = sourceId!;

        // ASSERT
        Assert.Equal("Título anterior", viewModel.SourceTitle);

        // Verifies that an empty or null SourceId does not overwrite the existing source title.
    }
    [Fact]
    public async Task LoadNews_WhenAllCategoriesAreInactive_SetsContentEmpty()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología",
            "Ciencia"
        }
        };

        var preferences = new TestPreferencesService();

        preferences.Set(
        "disabled_categories",
        """["Tecnología","Ciencia"]""");

        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = "elpais";

        viewModel.NewsItems.Add(new NewsItem
        {
            Title = "Noticia anterior"
        });

        // ACT
        await viewModel.LoadNews();

        // ASSERT
        Assert.Empty(viewModel.NewsItems);
        Assert.True(viewModel.IsContentEmpty);
        Assert.False(viewModel.NoResultsFound);
        Assert.False(viewModel.CanLoadMore);
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.IsRefreshing);

        // Verifies that LoadNews clears existing content and marks the ViewModel as empty when all categories are inactive.
    }
    [Fact]
    public async Task LoadNews_WhenAlreadyLoading_DoesNothing()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología"
        }
        };

        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.IsLoading = true;
        viewModel.IsContentEmpty = true;

        viewModel.NewsItems.Add(new NewsItem
        {
            Title = "Noticia existente"
        });

        // ACT
        await viewModel.LoadNews();

        // ASSERT
        Assert.True(viewModel.IsLoading);
        Assert.True(viewModel.IsContentEmpty);
        Assert.Single(viewModel.NewsItems);
        Assert.Equal("Noticia existente", ((NewsItem)viewModel.NewsItems[0]).Title);

        // Verifies that LoadNews exits immediately when a load operation is already in progress.
    }
    [Fact]
    public async Task LoadMore_WhenCannotLoadMore_DoesNothing()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.CanLoadMore = false;
        viewModel.IsLoadingMore = false;

        viewModel.NewsItems.Add(new NewsItem
        {
            Title = "Noticia existente"
        });

        // ACT
        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        // ASSERT
        Assert.False(viewModel.IsLoadingMore);
        Assert.Single(viewModel.NewsItems);
        Assert.Equal("Noticia existente", ((NewsItem)viewModel.NewsItems[0]).Title);

        // Verifies that LoadMore exits immediately when there are no more items available to load.
    }
    [Fact]
    public async Task LoadMore_WhenLoading_DoesNothing()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.IsLoading = true;
        viewModel.IsLoadingMore = false;
        viewModel.CanLoadMore = true;

        viewModel.NewsItems.Add(new NewsItem
        {
            Title = "Noticia existente"
        });

        // ACT
        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        // ASSERT
        Assert.True(viewModel.IsLoading);
        Assert.False(viewModel.IsLoadingMore);
        Assert.Single(viewModel.NewsItems);
        Assert.Equal("Noticia existente", ((NewsItem)viewModel.NewsItems[0]).Title);

        // Verifies that LoadMore exits immediately when the main news load operation is in progress.
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnAppearingAsync_WhenSourceIdIsEmpty_DoesNothing(string? sourceId)
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = sourceId!;

        // ACT
        await viewModel.OnAppearingAsync();

        // ASSERT
        Assert.False(viewModel.IsLoading);
        Assert.Empty(viewModel.NewsItems);

        // Verifies that OnAppearingAsync does nothing when SourceId is null, empty, or whitespace.
    }
    [Fact]
    public async Task OnAppearingAsync_WhenAlreadyLoaded_DoesNothing()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología",
            "Ciencia"
        }
        };

        var preferences = new TestPreferencesService();

        preferences.Set(
            "disabled_categories",
            """["Tecnología","Ciencia"]""");

        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = "elpais";

        // First appearance marks the ViewModel as loaded.
        await viewModel.OnAppearingAsync();

        viewModel.NewsItems.Add(new NewsItem
        {
            Title = "Noticia existente"
        });

        // ACT
        await viewModel.OnAppearingAsync();

        // ASSERT
        Assert.Single(viewModel.NewsItems);
        Assert.Equal("Noticia existente", ((NewsItem)viewModel.NewsItems[0]).Title);

        // Verifies that OnAppearingAsync does not load the news again after the ViewModel has already been loaded.
    }
    [Fact]
    public async Task LoadNews_WhenAnExceptionOccurs_SetsErrorState()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            ExceptionToThrow = new InvalidOperationException("Test error")
        };

        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = "elpais";

        // ACT
        await viewModel.LoadNews();

        // ASSERT
        Assert.True(viewModel.HasError);
        Assert.Equal(
            "No se pudieron cargar las noticias. Comprueba tu conexión e inténtalo de nuevo.",
            viewModel.ErrorMessage);
        Assert.False(viewModel.IsContentEmpty);
        Assert.False(viewModel.NoResultsFound);
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.IsRefreshing);

        // Verifies that LoadNews sets the error state and resets loading flags when an exception occurs.
    }
    [Fact]
    public async Task LoadNews_WhenStartingNewLoad_ResetsPreviousState()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología"
        }
        };

        var preferences = new TestPreferencesService();

        preferences.Set(
            "disabled_categories",
            """["Tecnología"]""");

        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = "elpais";

        viewModel.HasError = true;
        viewModel.ErrorMessage = "Error anterior";
        viewModel.IsContentEmpty = true;
        viewModel.NoResultsFound = true;

        // ACT
        await viewModel.LoadNews();

        // ASSERT
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
        Assert.True(viewModel.IsContentEmpty);
        Assert.False(viewModel.NoResultsFound);

        // Verifies that LoadNews clears previous error and result states before starting a new load.
    }
    [Fact]
    public async Task LoadNews_WhenStartingNewLoad_ResetsCanLoadMore()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología"
        }
        };

        var preferences = new TestPreferencesService();

        preferences.Set(
            "disabled_categories",
            """["Tecnología"]""");

        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = "elpais";
        viewModel.CanLoadMore = true;

        // ACT
        await viewModel.LoadNews();

        // ASSERT
        Assert.False(viewModel.CanLoadMore);
        Assert.False(viewModel.IsLoading);

        // Verifies that LoadNews resets the pagination state before processing a new load.
    }
    [Fact]
    public async Task LoadNews_WhenRefreshing_DoesNotSetIsLoading()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología"
        }
        };

        var preferences = new TestPreferencesService();

        preferences.Set(
            "disabled_categories",
            """["Tecnología"]""");

        var filterService = new FilterPreferencesService(preferences);
        var adInterleaveService = new AdInterleaveService();
        var logger = new TestLogger();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new NewsBySourceViewModel(
            aggregator,
            filterService,
            adInterleaveService,
            logger,
            sourceCatalog);

        viewModel.SourceId = "elpais";
        viewModel.IsRefreshing = true;

        // ACT
        await viewModel.LoadNews();

        // ASSERT
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.IsRefreshing);
        Assert.True(viewModel.IsContentEmpty);

        // Verifies that LoadNews does not enable IsLoading during a refresh and resets IsRefreshing when the operation finishes.
    }
    // LoadNews branches that depend directly on MAUI platform services
    // (Connectivity.Current and MainThread.BeginInvokeOnMainThread) cannot
    // be unit-tested in the plain .NET test runner without modifying production code.
    // CASO 1 and the error/state-reset paths remain covered by the tests above.
}
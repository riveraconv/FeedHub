using FeedHub_App.ViewModels;
using FeedHub_Core.Services;
using FeedHub.App.Tests.Helpers;
using FeedHub_Core.Models;

namespace FeedHub.App.Tests.ViewModels;

public class FilterViewModelTests
{
    [Fact]
    public void Constructor_InitializesExpectedState()
    {
        // ARRANGE
        var filterService = new FilterPreferencesService(new TestPreferencesService());
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        // ACT
        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ASSERT
        Assert.True(viewModel.ShowCategories);
        Assert.Empty(viewModel.Categories);
        Assert.Empty(viewModel.Sources);

        // Verifies that the constructor initializes the ViewModel with its expected default state.
    }
    [Fact]
    public void ChangeTab_WithCategoriesTab_ShowsCategories()
    {
        // ARRANGE
        var filterService = new FilterPreferencesService(new TestPreferencesService());
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ACT
        viewModel.ChangeTabCommand.Execute("cat");

        // ASSERT
        Assert.True(viewModel.ShowCategories);

        // Verifies that selecting the categories tab enables the categories view.
    }
    [Fact]
    public void ChangeTab_WithSourcesTab_HidesCategories()
    {
        // ARRANGE
        var filterService = new FilterPreferencesService(new TestPreferencesService());
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ACT
        viewModel.ChangeTabCommand.Execute("src");

        // ASSERT
        Assert.False(viewModel.ShowCategories);

        // Verifies that selecting the sources tab hides the categories view.
    }
    [Fact]
    public void SavePreference_WithCategory_SavesCategoryPreference()
    {
        // ARRANGE
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        var item = new FilterItem
        {
            Code = "Tecnología",
            IsActive = false
        };

        // ACT
        viewModel.SavePreference(item, true);

        // ASSERT
        Assert.False(filterService.IsCategoryActive("Tecnología"));

        // Verifies that saving a category updates its active preference.
    }
    [Fact]
    public void SavePreference_WithSource_SavesSourcePreference()
    {
        // ARRANGE
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        var item = new FilterItem
        {
            Code = "elpais",
            IsActive = false
        };

        // ACT
        viewModel.SavePreference(item, false);

        // ASSERT
        Assert.False(filterService.IsSourceActive("elpais"));

        // Verifies that saving a source updates its active preference.
    }
    [Fact]
    public void LoadFiltersIfNeeded_WhenCollectionsAreEmpty_LoadsSources()
    {
        // ARRANGE
        var filterService = new FilterPreferencesService(new TestPreferencesService());
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ACT
        viewModel.LoadFiltersIfNeeded();

        // ASSERT
        Assert.NotEmpty(viewModel.Sources);

        // Verifies that filters are loaded when the collections are initially empty.
    }
    [Fact]
    public void LoadFiltersIfNeeded_WhenCollectionsAlreadyContainItems_DoesNotReload()
    {
        // ARRANGE
        var filterService = new FilterPreferencesService(new TestPreferencesService());
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        viewModel.Sources.Add(new FilterItem
        {
            Title = "Existing source",
            Code = "existing",
            IsActive = true
        });

        // ACT
        viewModel.LoadFiltersIfNeeded();

        // ASSERT
        Assert.Single(viewModel.Sources);
        Assert.Equal("existing", viewModel.Sources[0].Code);

        // Verifies that filters are not reloaded when at least one collection already contains items.
    }
    [Fact]
    public void LoadFiltersIfNeeded_LoadsSourcesWithExpectedValues()
    {
        // ARRANGE
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var aggregator = new TestNewsAggregatorService();
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ACT
        viewModel.LoadFiltersIfNeeded();

        // ASSERT
        var source = viewModel.Sources.First(s => s.Code == "elpais");

        Assert.Equal("El País", source.Title);
        Assert.Equal("elpais", source.Code);
        Assert.True(source.IsActive);

        // Verifies that source data is correctly mapped to FilterItem properties when filters are loaded.
    }
    [Fact]
    public void LoadFiltersIfNeeded_LoadsCategoriesWithExpectedValues()
    {
        // ARRANGE
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología"
        }
        };
        var sourceCatalog = new SourceCatalogService();

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ACT
        viewModel.LoadFiltersIfNeeded();

        // ASSERT
        var category = viewModel.Categories.Single();

        Assert.Equal("TECNOLOGÍA", category.Title);
        Assert.Equal("Tecnología", category.Code);
        Assert.True(category.IsActive);

        // Verifies that category data is correctly mapped to FilterItem properties when filters are loaded.
    }
    [Fact]
    public void LoadFiltersIfNeeded_WithInactiveCategory_LoadsCategoryAsInactive()
    {
        // ARRANGE
        var preferences = new TestPreferencesService();
        var filterService = new FilterPreferencesService(preferences);
        var aggregator = new TestNewsAggregatorService
        {
            AvailableCategories = new List<string>
        {
            "Tecnología"
        }
        };
        var sourceCatalog = new SourceCatalogService();

        filterService.SetCategoryActive("Tecnología", false);

        var viewModel = new FilterViewModel(
            filterService,
            aggregator,
            sourceCatalog);

        // ACT
        viewModel.LoadFiltersIfNeeded();

        // ASSERT
        var category = viewModel.Categories.Single();

        Assert.False(category.IsActive);

        // Verifies that a category saved as inactive is loaded with the inactive state.
    }

    // FilterViewModel tests cover:
    // - Constructor initialization and default state.
    // - Switching between categories and sources tabs.
    // - Saving category and source preferences.
    // - Loading filters when collections are empty.
    // - Preventing filter reload when collections already contain items.
    // - Mapping source data to FilterItem properties.
    // - Mapping category data to FilterItem properties.
    // - Loading categories with their saved active/inactive state.
}
using FeedHub.App.Tests.Helpers;
using FeedHub_App.ViewModels.News;
using FeedHub_Core.Models;
using FeedHub_Core.Services;


namespace FeedHub.App.Tests.ViewModels.News;

public class LatestNewsViewModelTests
{
    private static LatestNewsViewModel CreateViewModel()
    {
        return new LatestNewsViewModel(
            new TestNewsAggregatorService(),
            new TestLogger(),
            new AdInterleaveService());
    }

    [Fact]
    public void Constructor_InitializesExpectedState()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        // ASSERT
        Assert.Empty(viewModel.News);
        Assert.False(viewModel.IsRefreshing);
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.IsLoadingMore);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
        Assert.False(viewModel.IsInitialLoadComplete);
        Assert.False(viewModel.CanLoadMore);
        Assert.False(viewModel.IsContentEmpty);
        Assert.False(viewModel.IsLatestEmpty);
        Assert.Equal(NewsQueryStatus.Success, viewModel.EmptyStatus);
        Assert.True(viewModel.HasContent);
    }
    // Verifies that the ViewModel initializes all state properties with their expected default values.

    [Fact]
    public void HasContent_WhenLoading_IsFalse()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        // ACT
        viewModel.IsLoading = true;

        // ASSERT
        Assert.False(viewModel.HasContent);
    }
    // Verifies that content is considered unavailable while news are loading.

    [Fact]
    public void HasContent_WhenHasError_IsFalse()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        // ACT
        viewModel.HasError = true;

        // ASSERT
        Assert.False(viewModel.HasContent);
    }
    // Verifies that content is considered unavailable when the ViewModel has an error.

    [Fact]
    public void HasContent_WhenContentIsEmpty_IsFalse()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        // ACT
        viewModel.IsContentEmpty = true;

        // ASSERT
        Assert.False(viewModel.HasContent);
    }
    // Verifies that content is considered unavailable when the content is empty.

    [Fact]
    public void HasContent_WhenLoadingErrorAndEmptyAreFalse_IsTrue()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        // ACT
        viewModel.IsLoading = false;
        viewModel.HasError = false;
        viewModel.IsContentEmpty = false;

        // ASSERT
        Assert.True(viewModel.HasContent);
    }
    // Verifies that content is available when the ViewModel is not loading, has no error, and is not empty.

    [Fact]
    public async Task LoadNewsAsync_WhenAlreadyLoading_DoesNothing()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var viewModel = new LatestNewsViewModel(
            aggregator,
            new TestLogger(),
            new AdInterleaveService());

        viewModel.IsLoading = true;

        // ACT
        await viewModel.LoadNewsAsync();

        // ASSERT
        Assert.True(viewModel.IsLoading);
        Assert.Empty(viewModel.News);
    }
    // Verifies that loading is ignored when another load operation is already in progress.

    [Fact]
    public async Task LoadNewsAsync_WhenRefreshing_DoesNothing()
    {
        // ARRANGE
        var aggregator = new TestNewsAggregatorService();
        var viewModel = new LatestNewsViewModel(
            aggregator,
            new TestLogger(),
            new AdInterleaveService());

        viewModel.IsRefreshing = true;

        // ACT
        await viewModel.LoadNewsAsync();

        // ASSERT
        Assert.True(viewModel.IsRefreshing);
        Assert.Empty(viewModel.News);
    }
    // Verifies that loading is ignored while the ViewModel is refreshing.

    [Fact]
    public async Task LoadMoreAsync_WhenLoading_DoesNothing()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        viewModel.IsLoading = true;
        viewModel.CanLoadMore = true;

        // ACT
        await viewModel.LoadMoreAsync();

        // ASSERT
        Assert.True(viewModel.IsLoading);
        Assert.Empty(viewModel.News);
    }
    // Verifies that loading more news is ignored while the initial load is in progress.

    [Fact]
    public async Task LoadMoreAsync_WhenAlreadyLoadingMore_DoesNothing()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        viewModel.IsLoadingMore = true;
        viewModel.CanLoadMore = true;

        // ACT
        await viewModel.LoadMoreAsync();

        // ASSERT
        Assert.True(viewModel.IsLoadingMore);
        Assert.Empty(viewModel.News);
    }
    // Verifies that a second LoadMore operation is ignored while another one is already running.

    [Fact]
    public async Task LoadMoreAsync_WhenCannotLoadMore_DoesNothing()
    {
        // ARRANGE
        var viewModel = CreateViewModel();

        viewModel.CanLoadMore = false;

        // ACT
        await viewModel.LoadMoreAsync();

        // ASSERT
        Assert.False(viewModel.IsLoadingMore);
        Assert.Empty(viewModel.News);
    }
    // Verifies that loading more news is ignored when there are no more items available.


// LatestNewsViewModel tests cover the constructor state, HasContent behavior,
// and guard clauses for LoadNewsAsync and LoadMoreAsync.
//
// The remaining execution paths cannot be unit-tested in the plain .NET test runner
// because they depend directly on MAUI platform services such as Connectivity.Current,
// MainThread.BeginInvokeOnMainThread, and Shell.Current.
// Testing those paths would require MAUI platform infrastructure or production changes
// to abstract those dependencies.

}
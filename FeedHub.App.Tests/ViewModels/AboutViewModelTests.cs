using FeedHub_App.ViewModels;

namespace FeedHub.App.Tests.ViewModels;

public class AboutViewModelTests
{
    [Fact]
    public void Constructor_InitializesExpectedState()
    {
        // ARRANGE & ACT
        var viewModel = new AboutViewModel();

        // ASSERT
        Assert.True(viewModel.ShowApp);

        // Verifies that the ViewModel initializes with the app tab selected.
    }
    [Fact]
    public void ChangeTab_WithAppTab_ShowsApp()
    {
        // ARRANGE
        var viewModel = new AboutViewModel();

        // ACT
        viewModel.ChangeTabCommand.Execute("app");

        // ASSERT
        Assert.True(viewModel.ShowApp);

        // Verifies that selecting the app tab shows the app information.
    }
    [Fact]
    public void ChangeTab_WithOtherTab_HidesApp()
    {
        // ARRANGE
        var viewModel = new AboutViewModel();

        // ACT
        viewModel.ChangeTabCommand.Execute("other");

        // ASSERT
        Assert.False(viewModel.ShowApp);

        // Verifies that selecting a tab other than the app tab hides the app information.
    }

    // AboutViewModel tests cover:
    // - Constructor initialization and default state.
    // - Selecting the app tab.
    // - Selecting any other tab.
}
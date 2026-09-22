using FeedHub_App.ViewModels.News;

namespace FeedHub.App.Tests.ViewModels.News;

public class QuickViewViewModelTests
{
    [Fact]
    public void SpeakNews_WhenNewsContentIsEmpty_DoesNothing()
    {
        // ARRANGE
        var viewModel = new QuickViewViewModel();

        // ACT
        viewModel.SpeakNews();

        // ASSERT
        Assert.Equal("🔊", viewModel.SpeakIcon);

        // Verifies that speech is not started when there is no news content.
    }
    [Fact]
    public void StopSpeaking_ResetsSpeakIcon()
    {
        // ARRANGE
        var viewModel = new QuickViewViewModel
        {
            SpeakIcon = "🔇"
        };

        // ACT
        viewModel.StopSpeaking();

        // ASSERT
        Assert.Equal("🔊", viewModel.SpeakIcon);

        // Verifies that stopping speech always resets the speech icon.
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SpeakNews_WhenNewsContentIsNullOrWhiteSpace_DoesNothing(string? content)
    {
        // ARRANGE
        var viewModel = new QuickViewViewModel
        {
            NewsContent = content
        };

        // ACT
        viewModel.SpeakNews();

        // ASSERT
        Assert.Equal("🔊", viewModel.SpeakIcon);

        // Verifies that speech is not started when news content is null, empty, or whitespace.
    }

    // QuickViewViewModel tests cover the logic that can be exercised without MAUI platform services.
    // Speech execution, cancellation, TTS error handling, and MainThread callbacks are not unit-tested
    // because they depend directly on TextToSpeech.Default and MainThread.
}
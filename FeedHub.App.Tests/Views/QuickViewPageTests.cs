using FeedHub_App.Views.News;

namespace FeedHub.App.Tests.Views;

public class QuickViewPageTests
{
    [Theory]
    [InlineData(null, "fallback", "fallback")]
    [InlineData("", "fallback", "fallback")]
    [InlineData("   ", "fallback", "fallback")]
    [InlineData("https%3A%2F%2Fexample.com%2Ftest", "fallback", "https://example.com/test")]
    public void NormalizeQueryValue_ReturnsExpectedValue(string? input, string fallback, string expected)
    {
        var result = QuickViewPage.NormalizeQueryValue(input, fallback);

        Assert.Equal(expected, result);
    }
}

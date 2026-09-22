using FeedHub_App.Utilities;
using Microsoft.Maui.Controls;
using FeedHub_Core.Models;

namespace FeedHub.App.Tests.Utilities;

public class NewsTemplateSelectorTests
{
    [Fact]
    public void SelectTemplate_ReturnsAdTemplate_WhenItemIsAdItem()
    {
        // ARRANGE
        var adTemplate = new DataTemplate();
        var newsTemplate = new DataTemplate();

        var selector = new NewsTemplateSelector
        {
            AdTemplate = adTemplate,
            NewsTemplate = newsTemplate
        };

        var item = new AdItem();

        // ACT
        var result = selector.SelectTemplate(item, null!);

        // ASSERT
        Assert.Same(adTemplate, result);
        // Verifies that an AdItem selects the configured advertisement template.
    }
    [Fact]
    public void SelectTemplate_ReturnsNewsTemplate_WhenItemIsNewsItem()
    {
        // ARRANGE
        var adTemplate = new DataTemplate();
        var newsTemplate = new DataTemplate();

        var selector = new NewsTemplateSelector
        {
            AdTemplate = adTemplate,
            NewsTemplate = newsTemplate
        };

        var item = new NewsItem();

        // ACT
        var result = selector.SelectTemplate(item, null!);

        // ASSERT
        Assert.Same(newsTemplate, result);
        // Verifies that a non-AdItem selects the configured news template.
    }
}
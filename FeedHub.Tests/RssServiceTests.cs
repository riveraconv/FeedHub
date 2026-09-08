using FeedHub_Core.Utilities;

namespace FeedHub.Test;

public class RssServiceTests
{
    [Fact]
    public async Task GetNewsAsync_ReturnsEmptyValue()
    {
        // ARRANGE

        var logger = new TestLogger();
        var http = new HttpClient();
        var feedUrl = String.Empty;
        var categoryFromDict = String.Empty;
        CancellationToken ct = default;

        var rssService = new RssService(logger, http);

        // ACT

        var news = await rssService.GetNewsAsync(feedUrl, categoryFromDict, ct);

        // ASSERT

        Assert.Empty(news);
    }
}
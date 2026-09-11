using FeedHub_Core.Models;
using FeedHub_Core.Services;

namespace FeedHub.Tests;

public class AdInterleaveServiceTests()
{
    [Fact]
    public void Interleave_DoesNotAddAdWhenThereAreFewerThanFiveItems()
    {
        // ARRANGE

        var items = Enumerable.Range(1, 4)
            .Select(_ => new NewsItem())
            .ToList();

        var service = new AdInterleaveService();

        // ACT

        var result = service.Interleave(items);

        // ASSERT

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(result, item => item is AdItem);

        // Verifies that no ad is inserted when there are fewer than five news items.
    }
    [Fact]
    public void Interleave_AddsAdAfterFiveItems()
    {
        // ARRANGE

        var items = Enumerable.Range(1, 5)
            .Select(_ => new NewsItem())
            .ToList();

        var service = new AdInterleaveService();

        // ACT

        var result = service.Interleave(items);

        // ASSERT

        Assert.Equal(6, result.Count);
        Assert.Single(result.OfType<AdItem>());

        // Verifies that one ad is inserted after every five news items.
    }
    [Fact]
    public void Interleave_AddsAdAfterEveryFiveItems()
    {
        // ARRANGE

        var items = Enumerable.Range(1, 10)
            .Select(_ => new NewsItem())
            .ToList();

        var service = new AdInterleaveService();

        // ACT

        var result = service.Interleave(items);

        // ASSERT

        Assert.Equal(12, result.Count);
        Assert.Equal(2, result.OfType<AdItem>().Count());

        // Verifies that an ad is inserted after every five news items.
    }
    [Fact]
    public void Interleave_ReturnsEmptyListWhenThereAreNoItems()
    {
        // ARRANGE

        var items = Enumerable.Empty<NewsItem>();

        var service = new AdInterleaveService();

        // ACT

        var result = service.Interleave(items);

        // ASSERT

        Assert.Empty(result);

        // Verifies that no items or ads are returned when the input collection is empty.
    }
}
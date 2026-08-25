namespace FeedHub_Core.Models;

public class NewsQueryResult
{
    public List<NewsItem> Items { get; set; } = new();

    public NewsQueryStatus Status { get; set; }
}
public enum NewsQueryStatus
{
    Success,
    NoContent,
    FilteredOut,
    NoFeedsConfigured
}
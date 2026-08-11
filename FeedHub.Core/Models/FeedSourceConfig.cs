namespace FeedHub_Core.Models;

public class FeedSourceConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;

    public List<FeedConfig> Feeds { get; set; } = new();
}

public class FeedConfig
{
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
namespace FeedHub_Core.Models
{
    public class NewsItem
    {
        public string Title {  get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Description {  get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? CachedImage {  get; set; }
        public DateTime PublishDate {  get; set; }
        public string? Category {  get; set; }
        public string Source { get; set; } = string.Empty;
    }
}

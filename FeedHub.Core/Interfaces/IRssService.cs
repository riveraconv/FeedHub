using FeedHub_Core.Models;


namespace FeedHub_Core.Interfaces
{
    public interface IRssService
    {
        Task<List<NewsItem>> GetNewsAsync(string feedUrl);
    }
}

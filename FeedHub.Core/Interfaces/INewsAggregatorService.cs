
using FeedHub_Core.Models;

namespace FeedHub_Core.Services
{
    public interface INewsAggregatorService
    {
        Task<List<NewsItem>> GetLatestMixedAsync(int limit);
        Task<NewsQueryResult> GetByCategoryAsync(string category, int limit);
        Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit);
        Task<List<NewsItem>> GetBySourceAsync(string sourceId, int limit = 20);
        List<string> GetAvailableCategories();
        List<string> GetAvailableSources();
    }
    
}

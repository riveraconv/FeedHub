
using FeedHub_Core.Models;

namespace FeedHub_Core.Services
{
    public interface INewsAggregatorService
    {
        Task<NewsQueryResult> GetLatestMixedAsync(int limit);
        Task<NewsQueryResult> GetByCategoryAsync(string category, int limit);
        Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit);
        Task<NewsQueryResult> GetBySourceAsync(string sourceId, int limit = 20);
        List<string> GetAvailableCategories();
        List<string> GetAvailableSources();
    }
    
}

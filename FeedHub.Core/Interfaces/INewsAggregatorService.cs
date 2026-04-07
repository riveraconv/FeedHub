using FeedHub_Core.Models;
using FeedHub_Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedHub_Core.Services
{
    public interface INewsAggregatorService
    {
        Task<List<NewsItem>> GetLatestMixedAsync(int limit);
        Task<List<NewsItem>> GetByCategoryAsync(string category, int limit);
        Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit);
        Task<List<NewsItem>> GetBySourceAsync(string sourceId, int limit = 20);
    }
}

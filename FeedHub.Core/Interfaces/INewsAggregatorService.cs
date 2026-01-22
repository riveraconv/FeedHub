using FeedHub_Core.Models;
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
    }
}

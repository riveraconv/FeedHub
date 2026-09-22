using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Services;
using FeedHub_Core.Utilities;

namespace FeedHub.App.Tests.Helpers;

public class TestLogger : ILogger
{
    public void Info(string message, string caller = "")
    {
    }

    public void Warn(string message, string caller = "")
    {
    }

    public void Error(string message, string caller = "")
    {
    }
}

public class TestPreferencesService : IPreferencesService
{
    private readonly Dictionary<string, string> _values = new();

    public string Get(string key, string defaultValue)
    {
        return _values.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }

    public void Set(string key, string value)
    {
        _values[key] = value;
    }
}

public class TestNewsAggregatorService : INewsAggregatorService
{
    public List<string> AvailableCategories { get; set; } = new();
    public NewsQueryResult CategoryResult { get; set; } = new();

    public IEnumerable<NewsItem> SearchResults { get; set; }
        = Enumerable.Empty<NewsItem>();

    public Exception? ExceptionToThrow { get; set; }

    public Task<NewsQueryResult> GetLatestMixedAsync(int limit)
    {
        return Task.FromResult(new NewsQueryResult());
    }

    public Task<NewsQueryResult> GetByCategoryAsync(string category, int limit)
    {
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;

        return Task.FromResult(CategoryResult);
    }

    public Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit)
    {
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;

        return Task.FromResult(SearchResults);
    }

    public Task<NewsQueryResult> GetBySourceAsync(string sourceId, int limit = 20)
    {
        return Task.FromResult(new NewsQueryResult());
    }

    public List<string> GetAvailableCategories()
    {
        return AvailableCategories;
    }

    public List<string> GetAvailableSources()
    {
        return new();
    }
}
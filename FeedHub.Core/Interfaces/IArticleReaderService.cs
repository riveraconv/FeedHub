

namespace FeedHub_Core.Interfaces
{
    public interface IArticleReaderService
    {
        Task<string> GetCleanArticleAsync(string url);
        string GetCleanArticleSync(string url);
        Task<string> GetRawHtmlAsync(string url);
    }
}

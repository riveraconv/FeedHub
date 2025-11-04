using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedHub_Core.Interfaces
{
    public interface IArticleReaderService
    {
        Task<string> GetCleanArticleAsync(string url);
        string GetCleanArticleSync(string url);
        Task<string> GetRawHtmlAsync(string url);
    }
}

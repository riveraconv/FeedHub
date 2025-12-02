using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace FeedHub_Core.Utilities
{
    public interface ILogger
    {
        void Info(string message, [CallerMemberName] string caller = "");
        void Warn(string message, [CallerMemberName] string caller = "");
        void Error(string message, [CallerMemberName] string caller = "");
    }
}

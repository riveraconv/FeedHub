using FeedHub_Core.Utilities;
using System.Runtime.CompilerServices;
using System.Diagnostics;

#if ANDROID
using Android.Util;
#endif

namespace FeedHub_App.Utilities
{
    public class PlatformLogger : ILogger
    {
        private void Log (string message, string level, string caller)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formatted = $"[{timestamp}] [{caller}] {message}";

            System.Diagnostics.Debug.WriteLine($"[FH-GENERIC] {formatted}");

#if ANDROID

            switch (level)
            {
                case "INFO":
                    global::Android.Util.Log.Info("FeedHubLogger", formatted);
                    break;
                case "ERROR":
                    global::Android.Util.Log.Error("FeedHubLogger", formatted); 
                    break;
                case "WARN":
                    global::Android.Util.Log.Warn("FeedHubLogger", formatted);
                    break;
                default:
                    global::Android.Util.Log.Info("FeedHubLogger", formatted);
                    break;
            }
#elif WINDOWS
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                "INFO" => ConsoleColor.Blue,
                "ERROR" => ConsoleColor.Red,
                "WARN" => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };

            System.Diagnostics.Debug.WriteLine(formatted);

            Console.ForegroundColor = prevColor;
    
#endif
        }

        public void Info(string message, string caller = "") => Log(message, "INFO", caller);
        public void Warn(string message, string caller = "") => Log(message, "WARN", caller);
        public void Error(string message, string caller = "") => Log(message, "ERROR", caller);
    }
}


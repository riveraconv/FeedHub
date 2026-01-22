using Microsoft.Extensions.Logging;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Services;
using FeedHub_App.ViewModels.News;
using FeedHub_App.Views.News;
using FeedHub_App.Utilities;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;


namespace FeedHub_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            //Services
            builder.Services.AddHttpClient<IRssService, RssService>(client => {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FeedHub/1.0");
            });
            builder.Services.AddSingleton<IArticleReaderService, ArticleReaderService>();
            builder.Services.AddSingleton<QuickArticleResult>();
            builder.Services.AddSingleton<FeedHub_Core.Utilities.ILogger, PlatformLogger>();
            builder.Services.AddSingleton<INewsAggregatorService, NewsAggregatorService>();

            //ViewModels
            builder.Services.AddSingleton<LatestNewsViewModel>();
            builder.Services.AddSingleton<CategoryListViewModel>();

            //Pages
            builder.Services.AddSingleton<LatestNewsPage>();
            builder.Services.AddTransient<QuickViewPage>();
            builder.Services.AddTransient<CategoryListPage>();
            builder.Services.AddTransient<CategoryNewsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

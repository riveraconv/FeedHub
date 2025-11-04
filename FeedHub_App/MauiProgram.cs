using Microsoft.Extensions.Logging;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Services;
using FeedHub_App.ViewModels.News;
using FeedHub_App.Views.News;

namespace FeedHub_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            //Services
            builder.Services.AddSingleton<IRssService, RssService>();
            builder.Services.AddSingleton<IArticleReaderService, ArticleReaderService>();
            builder.Services.AddSingleton<QuickArticleResult>();

            //ViewModels
            builder.Services.AddSingleton<LatestNewsViewModel>();

            //Primary Pages
            builder.Services.AddSingleton<LatestNewsPage>();
            
            //Secondary Pages
            builder.Services.AddTransient<QuickViewPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

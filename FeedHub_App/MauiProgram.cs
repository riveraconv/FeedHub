using Microsoft.Extensions.Logging;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Services;
using FeedHub_App.ViewModels.News;
using FeedHub_App.Views.News;
using FeedHub_App.ViewModels.Settings;
using FeedHub_App.Views.Settings;
using FeedHub_App.Utilities;
using FeedHub_App.Views;
using FeedHub_App.ViewModels;
using CommunityToolkit.Maui;



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
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // --- SERVICIOS ---
            builder.Services.AddHttpClient<IRssService, RssService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.All,
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 10,
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    });

            builder.Services.AddSingleton<IArticleReaderService, ArticleReaderService>();
            builder.Services.AddSingleton<QuickArticleResult>();
            builder.Services.AddSingleton<FeedHub_Core.Utilities.ILogger, PlatformLogger>();
            builder.Services.AddSingleton<INewsAggregatorService, NewsAggregatorService>();

            // --- VIEWMODELS ---

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<LatestNewsViewModel>();
            builder.Services.AddTransient<CategoryListViewModel>();
            builder.Services.AddTransient<QuickViewViewModel>();
            builder.Services.AddTransient<CategoryNewsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // --- PAGES ---

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LatestNewsPage>();
            builder.Services.AddTransient<QuickViewPage>();
            builder.Services.AddTransient<CategoryListPage>();
            builder.Services.AddTransient<CategoryNewsPage>();
            builder.Services.AddTransient<SettingsPage>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}


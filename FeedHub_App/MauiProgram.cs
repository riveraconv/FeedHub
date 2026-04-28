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
using System.Diagnostics;
using Microsoft.Maui.Platform;
using FeedHub_App.Services;

namespace FeedHub_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            try
            {
                
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureMauiHandlers(handlers =>
                {

#if ANDROID
                    handlers.AddHandler<Microsoft.Maui.Controls.WebView, Platforms.Android.Handlers.CustomWebViewHandler>();

#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("fa-solid.otf", "FontAwesome");
                });
#if ANDROID
                //handler para cambiar los colores de la SearchBar de Categories
                Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("CustomSearchVisuals", (handler, view) =>
            {
                var searchView = handler.PlatformView;

                // 1. Color del Cursor (Blanco)
                int searchEditTextId = searchView.Context.Resources.GetIdentifier("android:id/search_src_text", null, null);
                var editText = searchView.GetChildrenOfType<Android.Widget.EditText>().FirstOrDefault();
                if (editText != null)
                {
                    editText.TextCursorDrawable?.SetTint(Android.Graphics.Color.White);
                }
            });
#endif

            // --- SERVICIOS ---
            builder.Services.AddHttpClient<IRssService, RssService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.All,
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 10,
                    });

            builder.Services.AddSingleton<IArticleReaderService, ArticleReaderService>();
            builder.Services.AddSingleton<QuickArticleResult>();
            builder.Services.AddSingleton<FeedHub_Core.Utilities.ILogger, PlatformLogger>();
            builder.Services.AddSingleton<INewsAggregatorService, NewsAggregatorService>();
            builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();
            builder.Services.AddSingleton<FilterPreferencesService>();
            
            
            // --- VIEWMODELS ---

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddTransient<LatestNewsViewModel>();
            builder.Services.AddTransient<CategoryListViewModel>();
            builder.Services.AddSingleton<CategoriesBySourceViewModel>();
            builder.Services.AddTransient<QuickViewViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddTransient<CategoryNewsViewModel>();
            builder.Services.AddTransient<NewsBySourceViewModel>();
            builder.Services.AddTransient<FilterViewModel>();

            // --- PAGES ---

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<LatestNewsPage>();
            builder.Services.AddTransient<QuickViewPage>();
            builder.Services.AddTransient<CategoryListPage>();
            builder.Services.AddSingleton<CategoriesNewsBySourcePage>();
            builder.Services.AddTransient<CategoryNewsPage>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddTransient<NewsBySourcePage>();
            builder.Services.AddTransient<SelectFilterPage>();



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
            var app = builder.Build();
                System.Diagnostics.Debug.WriteLine("DEBUG: MauiApp construido correctamente");
                return app;
            }
            catch(Exception ex)
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "startup_crash.log"),
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}


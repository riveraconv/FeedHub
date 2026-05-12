using FeedHub_App.Views.News;
using FeedHub_App.Views.Settings;
using FeedHub_App.Views;


namespace FeedHub_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LatestNewsPage), typeof(LatestNewsPage));
            Routing.RegisterRoute(nameof(CategoryListPage), typeof(CategoryListPage));
            Routing.RegisterRoute(nameof(CategoryNewsPage), typeof(CategoryNewsPage));
            Routing.RegisterRoute(nameof(NewsDetailPage), typeof(NewsDetailPage));
            Routing.RegisterRoute(nameof(QuickViewPage), typeof(QuickViewPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(CategoriesNewsBySourcePage), typeof(CategoriesNewsBySourcePage));
            Routing.RegisterRoute(nameof(NewsBySourcePage), typeof(NewsBySourcePage));
            Routing.RegisterRoute(nameof(SelectFilterPage), typeof(SelectFilterPage));
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        }
    }
}

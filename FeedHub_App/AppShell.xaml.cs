using FeedHub_App.Views.News;
using FeedHub_App.Views.Settings;

namespace FeedHub_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            //Lateral Navigation
            Routing.RegisterRoute(nameof(LatestNewsPage), typeof(LatestNewsPage));
            Routing.RegisterRoute(nameof(CategoryListPage), typeof(CategoryListPage));
            Routing.RegisterRoute(nameof(CategoryNewsPage), typeof(CategoryNewsPage));
            Routing.RegisterRoute(nameof(NewsDetailPage), typeof(NewsDetailPage));
            Routing.RegisterRoute(nameof(QuickViewPage), typeof(QuickViewPage));
  
            //Modal Navigation
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        }
    }
}

using FeedHub_App.Views.News;

namespace FeedHub_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(NewsDetailPage), typeof(NewsDetailPage));
            Routing.RegisterRoute(nameof(QuickViewPage), typeof(QuickViewPage));
            Routing.RegisterRoute(nameof(CategoryListPage), typeof(CategoryListPage));
            Routing.RegisterRoute(nameof(CategoryNewsPage), typeof(CategoryNewsPage));
        }
    }
}

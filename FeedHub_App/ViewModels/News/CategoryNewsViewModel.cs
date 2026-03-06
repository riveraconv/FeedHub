using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Models;

namespace FeedHub_App.ViewModels.News
{
    public partial class CategoryNewsViewModel : ObservableObject
    {
        [RelayCommand]
        public async Task GoBackToCategoryList()
        {
            // Asegúrate de que en AppShell.xaml la ruta se llame exactamente así
            await Shell.Current.GoToAsync("//CategoryListPage");
        }

        [RelayCommand]
        public async Task OpenNews(NewsItem item)
        {
            if (item == null) return;

            // Aquí navegas al detalle de la noticia
            // Ajusta "NewsDetailPage" al nombre real de tu página de detalle
            await Shell.Current.GoToAsync($"NewsDetailPage?url={Uri.EscapeDataString(item.Source)}");
        }
    }
}
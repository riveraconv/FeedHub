using System.Collections.ObjectModel;
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
            await Shell.Current.GoToAsync("//CategoryListPage");
        }

        [RelayCommand]
        public async Task OpenNews(NewsItem item)
        {
            if (item == null) return;
            await Shell.Current.GoToAsync($"NewsDetailPage?url={Uri.EscapeDataString(item.Source)}");
        }
    }
}
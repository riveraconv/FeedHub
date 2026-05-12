using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FeedHub_App.Views.News;
using FeedHub_App.Views.Settings;
using FeedHub_App.Views;

namespace FeedHub_App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        string appVersion;

        public MainViewModel()
        {
            appVersion = $"V.{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";
        }
        [RelayCommand]
        private async Task GoToLatestNews()
        {
            // Navegamos de forma sencilla
            await Shell.Current.GoToAsync(nameof(LatestNewsPage));
        }

        [RelayCommand]
        private async Task GoToCategory()
        {
            await Shell.Current.GoToAsync(nameof(CategoryListPage), animate: true);
        }

        [RelayCommand]
        private async Task GoToSettings()
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }

        [RelayCommand]
        private async Task GoToCategoriesBySource()
        {
            await Shell.Current.GoToAsync(nameof(CategoriesNewsBySourcePage));
        }
        [RelayCommand]
        private async Task GoToAbout()
        {
            await Shell.Current.GoToAsync(nameof(AboutPage));
        }
    }
}




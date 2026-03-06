using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

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
            // Quitamos los "///". Si la página es hija de la actual o está registrada:
            await Shell.Current.GoToAsync("LatestNewsPage");
        }

        [RelayCommand]
        private async Task GoToCategory()
        {
            // OJO: Asegúrate de que el nombre coincida con el RegisterRoute
            await Shell.Current.GoToAsync("CategoryListPage");
        }

        [RelayCommand]
        private async Task GoToSettings()
        {
            await Shell.Current.GoToAsync("SettingsPage");
        }
    }
}




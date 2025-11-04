using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using FeedHub_App.Views.News;
using FeedHub_App.Views.Settings;

namespace FeedHub_App.ViewModels
{
    public partial class MainViewModel
    {
        public ICommand GoToLatestNewsCommand { get; }
        public ICommand GoToExploreCommand { get; }
        public ICommand GoToSettingsCommand { get; }
        public string AppVersion { get; }

        public MainViewModel()
        {
            AppVersion = $"V.{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";

            GoToLatestNewsCommand = new AsyncRelayCommand(LatestOnClicked);
            GoToExploreCommand = new AsyncRelayCommand(ExploreOnClicked);
            GoToSettingsCommand = new AsyncRelayCommand(SettingsOnClicked);
        }
        private async Task LatestOnClicked()
        {
            await Shell.Current.GoToAsync("///LatestNewsPage");
        }
        private async Task ExploreOnClicked()
        {
            await Shell.Current.GoToAsync("///ExplorePage");
        }
        private async Task SettingsOnClicked()
        {
            await Shell.Current.GoToAsync("///SettingsPage");
        }
    }
}




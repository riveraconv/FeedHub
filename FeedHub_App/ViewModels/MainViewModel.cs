using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace FeedHub_App.ViewModels
{
    public partial class MainViewModel
    {
        public ICommand GoToLatestNewsCommand { get; }
        public ICommand GoToExploreCommand { get; }
        public ICommand GoToSettingsCommand { get; }
        public ICommand GoBackCommand {  get; }
        public string AppVersion { get; }

        public MainViewModel()
        {
            AppVersion = $"V.{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";

            GoToLatestNewsCommand = new AsyncRelayCommand(LatestOnClicked);
            GoToExploreCommand = new AsyncRelayCommand(ExploreOnClicked);
            GoToSettingsCommand = new AsyncRelayCommand(SettingsOnClicked);
            GoBackCommand = new AsyncRelayCommand(GoBackOnClicked);
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
        private async Task GoBackOnClicked()
        {
            await Shell.Current.GoToAsync("///MainPage");
        }
    }
}




using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace FeedHub_App.ViewModels.Settings;

public partial class SettingsViewModel : ObservableObject
{
	public SettingsViewModel()
	{
	}

    [RelayCommand]
    public async Task GoBackToMainMenu()
    {
        await Shell.Current.GoToAsync("..");
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FeedHub_App.ViewModels;

public partial class AboutViewModel : ObservableObject
{
 	[ObservableProperty]
	private bool showApp = true;

	[RelayCommand]
	private void ChangeTab(string tab)
	{
		ShowApp = tab == "app";
	}
}

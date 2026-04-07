using FeedHub_App.ViewModels.News;
using FeedHub_Core.Models;

namespace FeedHub_App.Views.News;

public partial class NewsBySourcePage : ContentPage
{
	public NewsBySourcePage(NewsBySourceViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
	private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is NewsItem selectedItem)
		{
			((CollectionView)sender).SelectedItem = null;
			if (BindingContext is NewsBySourceViewModel vm)
			{
				await vm.SelectNewsCommand.ExecuteAsync(selectedItem);
			}
		}
	}
}
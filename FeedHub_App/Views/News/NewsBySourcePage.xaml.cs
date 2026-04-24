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
	private async void OnNewsClicked(object sender, EventArgs e)
	{
		if (sender is Button btn && btn.BindingContext is NewsItem item)
		{
			await ((NewsBySourceViewModel)BindingContext).SelectNewsCommand.ExecuteAsync(item);
		}
	}
	private async void OnLoadMoreClicked(object sender, EventArgs e)
	{
		await ((NewsBySourceViewModel)BindingContext).LoadMoreCommand.ExecuteAsync(null);
	}
}
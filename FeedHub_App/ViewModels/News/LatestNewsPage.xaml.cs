using FeedHub_App.ViewModels.News;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;

namespace FeedHub_App.Views.News;

public partial class LatestNewsPage : ContentPage
{
	private readonly LatestNewsViewModel _viewModel;
	public LatestNewsPage(IRssService rssService)
	{
		InitializeComponent();
		_viewModel = new LatestNewsViewModel(rssService);
		BindingContext = _viewModel;
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();
		_viewModel.LoadNewsCommand.Execute(null);
    }
    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is NewsItem selectedNews)
        {
            try
            {
                await Navigation.PushAsync(new NewsDetailPage(selectedNews.Link));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Was not posible to open the notice link: {ex.Message}", "OK");
            }
        }

        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;
    }
}
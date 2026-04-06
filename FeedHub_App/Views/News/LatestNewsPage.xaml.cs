using FeedHub_App.ViewModels.News;
using FeedHub_Core.Models;

namespace FeedHub_App.Views.News;

public partial class LatestNewsPage : ContentPage
{
    private readonly LatestNewsViewModel _viewModel;

    public LatestNewsPage(LatestNewsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is NewsItem selected)
        {
            _viewModel.OpenNewsCommand.Execute(selected);
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.News.Count == 0 && _viewModel.LoadNewsCommand.CanExecute(null))
        {
            _viewModel.LoadNewsCommand.Execute(null);
        }
    }
}


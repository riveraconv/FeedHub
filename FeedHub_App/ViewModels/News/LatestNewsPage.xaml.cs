using FeedHub_Core.Utilities;
using FeedHub_App.ViewModels.News;
using FeedHub_Core.Models;

namespace FeedHub_App.Views.News;

public partial class LatestNewsPage : ContentPage
{
    private readonly LatestNewsViewModel _viewModel;
    private readonly ILogger _logger;
    private bool _hasLoaded = false;

    public LatestNewsPage(LatestNewsViewModel viewModel, ILogger logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _logger = logger;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is NewsItem selected)
        {
            _viewModel.OpenNewsCommand.Execute(selected);
            ((CollectionView)sender).SelectedItem = null;
        }

        _logger.Info("Worked");
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasLoaded) return;
        _hasLoaded = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(50);
            if (_viewModel.LoadNewsCommand.CanExecute(null))
                _viewModel.LoadNewsCommand.Execute(null);
        });

        _logger.Info("Worked");
    }

}

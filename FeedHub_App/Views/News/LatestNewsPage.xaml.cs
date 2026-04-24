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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Leer logs de crash anteriores
        foreach (var logFile in new[] { "crash.log", "task_crash.log", "startup_crash.log" })
        {
            var path = System.IO.Path.Combine(FileSystem.AppDataDirectory, logFile);
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), logFile);
            }
            if (System.IO.File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($">>> {logFile}: {System.IO.File.ReadAllText(path)}");
                System.IO.File.Delete(path);
            }
        }

        if (_viewModel.News.Count == 0)
            await _viewModel.LoadNewsAsync();
    }
}


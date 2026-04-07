using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Models;
using FeedHub_Core.Services;
using System.Collections.ObjectModel;
using FeedHub_Core.Utilities;

namespace FeedHub_App.ViewModels.News
{
    public partial class LatestNewsViewModel : ObservableObject
    {
        private readonly INewsAggregatorService _aggregatorService;
        private readonly ILogger _logger;
        public ObservableCollection<NewsItem> News { get; } = new();

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isInitialLoadComplete = false;

        public LatestNewsViewModel(INewsAggregatorService aggregatorService)
        {
            _aggregatorService = aggregatorService;
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            if (News.Count > 0 && !IsRefreshing) return;

            try
            {
                // Si IsRefreshing es true (porque viene del pull-to-refresh),
                // mantenemos IsLoading en false para no mostrar el indicador central.
                if (!IsRefreshing)
                    IsLoading = true;

                var selected = await _aggregatorService.GetLatestMixedAsync(30);

                News.Clear();
                foreach (var item in selected) News.Add(item);
            }
            catch (Exception ex) { _logger?.Error(ex.Message); }
            finally
            {
                IsLoading = false;
                IsRefreshing = false; // Esto quita la ruedita de arriba del RefreshView
            }
        }

        [RelayCommand]
        private async Task OpenNewsAsync(NewsItem item)
        {
            if (item == null) return;

            await Shell.Current.GoToAsync(
                $"QuickViewPage?link={Uri.EscapeDataString(item.Link)}" +
                $"&title={Uri.EscapeDataString(item.Title)}" +
                $"&imageUrl={Uri.EscapeDataString(item.ImageUrl ?? string.Empty)}"+
                $"&source={Uri.EscapeDataString(item.Source ?? "Fuente")}");
        }
        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}


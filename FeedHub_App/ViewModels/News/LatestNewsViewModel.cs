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
        public ObservableCollection<NewsItem> News {get;} = new();

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isLoading;
        [ObservableProperty]
        private bool isLoadingMore;

        [ObservableProperty]
        private bool isInitialLoadComplete = false;
        private int _currentOffset = 0;
        private const int PageSize = 20;
        [ObservableProperty]
        private bool canLoadMore = false;
        public LatestNewsViewModel(INewsAggregatorService aggregatorService, ILogger logger)
        {
            _aggregatorService = aggregatorService;
            _logger = logger;
            News = new ObservableCollection<NewsItem>();
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            // Si ya estamos cargando (ya sea carga inicial o pull), abortamos
            if (IsLoading || IsRefreshing) return;

            try
            {
                // DETERMINAR QUÉ SPINNER SE MUESTRA:
                // Si la lista está vacía, es carga inicial -> Usamos IsLoading (Spinner central)
                // Si el usuario hizo "pull", IsRefreshing ya será true -> No entramos aquí
                if (News.Count == 0) IsLoading = true;

                _currentOffset = 0;
                var selected = await _aggregatorService.GetLatestMixedAsync(PageSize);

                MainThread.BeginInvokeOnMainThread(() => 
                {
                    News.Clear();
                    foreach(var item in selected) News.Add(item);
                    CanLoadMore = selected.Count >= PageSize;
                });
            }
            catch (Exception ex) 
            {
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsRefreshing = false;
                });
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
        public async Task LoadMoreAsync()
        {
            if (IsLoading || IsLoadingMore) return;
            try
            {
                IsLoadingMore = true;
                _currentOffset += PageSize;
                var more = await _aggregatorService.GetLatestMixedAsync(PageSize + _currentOffset);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existing = News.Select(n => n.Link).ToHashSet();
                    var newItems = more.Skip(_currentOffset).Where(n => !existing.Contains(n.Link)).ToList();

                    foreach (var item in newItems)
                    {
                        News.Add(item);
                    }
                    CanLoadMore = newItems.Count >= PageSize;
                });
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error cargando más noticias: {ex.Message}");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoadingMore = false;
                });
            }
        }
        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}


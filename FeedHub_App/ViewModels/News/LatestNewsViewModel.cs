using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Models;
using FeedHub_Core.Services;
using System.Collections.ObjectModel;
using FeedHub_Core.Utilities;
using FeedHub_App.Views.Settings;
using Microsoft.Maui.Networking;


namespace FeedHub_App.ViewModels.News
{
    public partial class LatestNewsViewModel : ObservableObject
    {
        private readonly INewsAggregatorService _aggregatorService;
        private readonly ILogger _logger;
        public ObservableCollection<object> News {get;} = new();

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isLoading;
        [ObservableProperty]
        private bool isLoadingMore;

        [ObservableProperty]
        private string errorMessage = string.Empty;
        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private bool isInitialLoadComplete = false;
        private int _currentOffset = 0;
        private const int PageSize = 20;
        [ObservableProperty]
        private bool canLoadMore = false;
        [ObservableProperty]
        private bool isContentEmpty = false;

        public bool HasContent =>
            !IsLoading &&
            !HasError &&
            !IsContentEmpty;

        private readonly AdInterleaveService _adService;
        private List<NewsItem> _fullListCache = new();
        public LatestNewsViewModel(INewsAggregatorService aggregatorService, ILogger logger, AdInterleaveService adService)
        {
            _aggregatorService = aggregatorService;
            _logger = logger;
            _adService = adService;
            News = new ObservableCollection<object>();
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            // Si ya estamos cargando (ya sea carga inicial o pull), abortamos
            if (IsLoading || IsRefreshing ) return;

            try
            {
                // DETERMINAR QUÉ SPINNER SE MUESTRA:
                // Si la lista está vacía, es carga inicial -> Usamos IsLoading (Spinner central)
                // Si el usuario hizo "pull", IsRefreshing ya será true -> No entramos aquí
                if (News.Count == 0) IsLoading = true;

                HasError = false;
                ErrorMessage = string.Empty;
                IsContentEmpty = false;

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    HasError = true;
                    ErrorMessage = "No hay conexión a Internet. Comprueba tu conexión e inténtalo de nuevo.";
                    return;
                }

                _currentOffset = 0;
                var selected = await _aggregatorService.GetLatestMixedAsync(100);
                _fullListCache = selected;

                var firstBatch = _fullListCache
                    .Take(PageSize)
                    .ToList();

                var mixed = _adService.Interleave(firstBatch);
                
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    News.Clear();
                    foreach(var item in mixed)
                    {
                        System.Diagnostics.Debug.WriteLine($"DEBUG ADDING: {item.GetType().Name}");
                        News.Add(item);
                    } 
                    _currentOffset = firstBatch.Count;
                    CanLoadMore = _fullListCache.Count > _currentOffset;
                    IsContentEmpty = News.Count == 0;

                });
            }
            catch (Exception ex) 
            {
                _logger?.Error($"Error cargando noticias: {ex.Message}");
                HasError = true;
                ErrorMessage = "No se pudieron cargar las noticias. Comprueba tu conexión e inténtalo de nuevo.";
                IsContentEmpty = false;
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
            _logger?.Info("Entrando en LoadMoreAsync");

            if (IsLoading || IsLoadingMore || !CanLoadMore) return;

            if(Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                HasError = true;
                ErrorMessage = "No hay conexión a Internet. Comprueba tu conexión e inténtalo de nuevo.";
                return;
            }
            try
            {
                HasError = false;
                ErrorMessage = string.Empty;
                IsLoadingMore = true;
                
                await Task.Delay(300);

                var existing = News
                    .OfType<NewsItem>()  // ← filtra solo NewsItem, ignora AdItem
                    .Select(n => n.Link)
                    .ToHashSet();

                    var nextItems = _fullListCache
                    .Skip(_currentOffset)
                    .Take(PageSize)
                    .Where(n => !existing.Contains(n.Link))
                    .ToList();

                    _currentOffset += nextItems.Count;

                    var mixed = _adService.Interleave(nextItems);

                    System.Diagnostics.Debug.WriteLine(
                        $"DEBUG LoadMore: añadiendo {nextItems.Count} noticias ({mixed.Count} elementos incluyendo anuncios). Offset actual: {_currentOffset}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in mixed)
                    {
                        News.Add(item);
                    }
                    CanLoadMore = _fullListCache.Count > _currentOffset;
                });
            }
            catch (Exception ex)
            {
                _logger.Warn($"No se pudieron cargar mas noticias {ex.Message}");

                HasError = true;
                ErrorMessage = $"No se pudieron cargar mas noticias. Comprueba tu conexión e inténtalo de nuevo.";
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
        [RelayCommand]
        public async Task GoToSettings()
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }

        //notificamos cuando cambia HasContent
        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(HasContent));
        }
        partial void OnHasErrorChanged(bool value)
        {
            OnPropertyChanged(nameof(HasContent));
        }
        partial void OnIsContentEmptyChanged(bool value)
        {
            OnPropertyChanged(nameof(HasContent));
        }
    }
}


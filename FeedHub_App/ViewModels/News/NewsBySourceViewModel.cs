using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FeedHub_Core.Services;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using FeedHub_App.Views.News;
using CommunityToolkit.Mvvm.Input;
using FeedHub_App.Views.Settings;

namespace FeedHub_App.ViewModels.News;

[QueryProperty(nameof(SourceId), "source")]
public partial class NewsBySourceViewModel : ObservableObject
{
    private readonly FilterPreferencesService _filterService;
    private readonly INewsAggregatorService _newsService;
    private readonly ILogger _logger;
    private readonly AdInterleaveService _adInterleaveService;
    private readonly SourceCatalogService _sourceCatalog;

    [ObservableProperty]
    string sourceId;
    [ObservableProperty]
    string sourceTitle;
    [ObservableProperty]
    private bool isRefreshing;
    [ObservableProperty]
    private bool isLoading;
    [ObservableProperty]
    private bool noResultsFound;
    [ObservableProperty]
    private bool isLoadingMore;
    private int _currentOffset = 0;
    private const int PageSize = 20;
    [ObservableProperty]
    private bool canLoadMore;
    [ObservableProperty]
    private bool isContentEmpty;

    [ObservableProperty]
    private bool hasError;
    [ObservableProperty]
    private string errorMessage = string.Empty;
    
    public ObservableCollection<object> NewsItems{get; set;} = new();
    private List<NewsItem> _fullListCache = new();

    public NewsBySourceViewModel(INewsAggregatorService newsService, FilterPreferencesService filterService, AdInterleaveService adInterleaveService,
                                ILogger logger, SourceCatalogService sourceCatalog)
    {
        _newsService = newsService;
        _filterService = filterService;
        _adInterleaveService = adInterleaveService;
        _logger = logger;
        _sourceCatalog = sourceCatalog;
        
    }
    partial void OnSourceIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SourceTitle = _sourceCatalog.GetSourceName(value);
        }
    }

    [RelayCommand]
    public async Task LoadNews()
    {
        _logger?.Info(">>> Entrando en LoadNews");
        
        if (IsLoading) return;

        //limpiamos siempre estados visuales anteriores

        HasError = false;
        ErrorMessage = string.Empty;
        IsContentEmpty = false;
        NoResultsFound = false;

        try
        {
            if (!IsRefreshing) 
                IsLoading = true;
            
            _currentOffset = 0;
            CanLoadMore = false;

            var activeCategories = _newsService
            .GetAvailableCategories()
            .Where(category => _filterService.IsCategoryActive(category))
            .ToHashSet();

            // CASO 1:
            // El usuario ha desactivado todas las categorías.
            if (activeCategories.Count == 0)
            {
                _fullListCache.Clear();
                NewsItems.Clear();

                IsContentEmpty = true;
                NoResultsFound = false;
                CanLoadMore = false;

                _logger?.Info(
                    $"Fuente '{SourceId}': todas las categorías están filtradas.");

                return;
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                _logger?.Info("Sin conexión. Cancelando LoadMore.");
                HasError = true;
                ErrorMessage = "No hay conexión a Internet. Comprueba tu conexión e inténtalo de nuevo.";
                return;
            }

            // La fuente seleccionada se consulta independientemente
            // de que esté activa o no en los filtros.
            var result = await _newsService.GetBySourceAsync(SourceId, 100);

            // CASO 2:
            // Hay categorías activas, pero la fuente no tiene contenido
            // disponible para ellas.
            if (result.Status == NewsQueryStatus.NoContent)
            {
                _fullListCache.Clear();
                NewsItems.Clear();

                CanLoadMore = false;
                IsContentEmpty = false;
                NoResultsFound = true;

                _logger?.Info(
                    $"Fuente '{SourceId}': no hay noticias disponibles " +
                    "para las categorías activas.");

                return;
            }

            // CASO 3:
            // Se ha producido algún problema con los feeds.
            // Normalmente GetBySourceAsync lanzará una excepción si
            // ninguno de los feeds ha podido cargarse.
            if (result.Status == NewsQueryStatus.FilteredOut)
            {
                // Este estado no debería producirse aquí porque
                // comprobamos las categorías activas antes de llamar
                // al servicio, pero lo dejamos cubierto.
                _fullListCache.Clear();
                NewsItems.Clear();

                CanLoadMore = false;
                IsContentEmpty = true;
                NoResultsFound = false;

                _logger?.Info(
                    $"Fuente '{SourceId}': contenido filtrado.");

                return;
            }
            /// CASO 4:
            // Tenemos contenido.
            _fullListCache = result.Items;

            var pagedItems = _fullListCache
                .Take(PageSize)
                .ToList();

            var mixedItems = _adInterleaveService.Interleave(pagedItems);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.Clear();

                foreach (var item in mixedItems)
                {
                    NewsItems.Add(item);
                }

                _currentOffset = pagedItems.Count;
                CanLoadMore = _fullListCache.Count > _currentOffset;

                IsContentEmpty = false;
                NoResultsFound = false;
            });

            _logger?.Info(
                $"Fuente '{SourceId}' | " +
                $"Noticias obtenidas: {_fullListCache.Count} | " +
                $"Primera página: {pagedItems.Count}");
        }
        catch (Exception ex)
        {
            _logger?.Error(
                $"Error cargando noticias por fuente: {ex.Message}");

            HasError = true;
            ErrorMessage =
                "No se pudieron cargar las noticias. Comprueba tu conexión e inténtalo de nuevo.";

            IsContentEmpty = false;
            NoResultsFound = false;
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        _logger?.Info("Entrando en LoadMore");

        // Si ya está cargando o no hay más, salimos
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
            IsLoadingMore = true; // Activamos estado específico para el footer

            _logger?.Info(">>> Voy a llamar a GetBySourceAsync");         
            await Task.Delay(300);

            var existingLinks = NewsItems
                .OfType<NewsItem>()  // ← filtra solo NewsItem
                .Select(n => n.Link)
                .ToHashSet();

            var newItems = _fullListCache
                .Skip(_currentOffset)
                .Take(PageSize)
                .Where(n => !existingLinks.Contains(n.Link))
                .ToList();

                var offsetBefore = _currentOffset;

            System.Diagnostics.Debug.WriteLine(
                $"DEBUG LoadMoreBySource: encontradas={newItems.Count}, " +
                $"offsetAntes={offsetBefore}");

            if (newItems.Any())
            {
                _currentOffset += newItems.Count;

                var mixedNewItems = _adInterleaveService.Interleave(newItems);

                System.Diagnostics.Debug.WriteLine(
                    $"DEBUG LoadMoreBySource: añadiendo={newItems.Count} noticias, " +
                    $"elementosTotales={mixedNewItems.Count}, " +
                    $"offsetDespues={_currentOffset}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in mixedNewItems)
                    {
                        NewsItems.Add(item);
                    }
                    CanLoadMore = _fullListCache.Count > _currentOffset;
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                   "DEBUG LoadMoreBySource: no hay noticias nuevas; se desactiva la paginación.");
                CanLoadMore = false;
            }
        }
        catch (Exception ex)
        {
            _logger?.Info(">>> Sin internet. Return.");
            _logger.Warn($"No se pudieron cargar mas noticias de '{SourceId}': {ex.Message}");

            HasError = true;
            ErrorMessage = "No se pudieron cargar más noticias. Comprueba tu conexión e inténtalo de nuevo.";
        }
        finally 
        { 
            IsLoadingMore = false; 
        }
    }

    [RelayCommand]
    private async Task SelectNews(NewsItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Link) || IsLoading) return;

        await Shell.Current.GoToAsync($"{nameof(QuickViewPage)}", new Dictionary<string, object>
        {
            { "link", item.Link },
            { "title", item.Title },
            { "imageUrl", item.ImageUrl ?? "" },
            { "source", item.Source ?? "Fuente" }
        });
    }
    [RelayCommand]
    private async Task GoToSettings()
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
    public async Task OnAppearingAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceId))
            return;

        await LoadNews();
    }
}

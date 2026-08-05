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
                                ILogger logger)
    {
        _newsService = newsService;
        _filterService = filterService;
        _adInterleaveService = adInterleaveService;
        _logger = logger;
    }
    partial void OnSourceIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SourceTitle = GetFriendlyName(value); 

            MainThread.BeginInvokeOnMainThread(async () => 
            {
                await Task.Delay(250); 
                if (LoadNewsCommand.CanExecute(null))
                {
                    await LoadNewsCommand.ExecuteAsync(null);
                }
            });
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


        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            _logger?.Info("Sin conexión. Cancelando LoadMore.");
            HasError = true;
            ErrorMessage = "No hay conexión a Internet. Comprueba tu conexión e inténtalo de nuevo.";
            return;
        }

        try
        {
            if (!IsRefreshing) 
                IsLoading = true;
            
            _currentOffset = 0;

            // Obtenemos los datos
            _fullListCache = await _newsService.GetBySourceAsync(SourceId, 100); 
            
            // Filtramos la primera página
            var pagedItems = _fullListCache.Take(PageSize).ToList();
            var mixedItems = _adInterleaveService.Interleave(pagedItems);

            System.Diagnostics.Debug.WriteLine($"DEBUG NewsBySource Items totales: {pagedItems.Count} | Tras mezcla: {mixedItems.Count}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.Clear();
                foreach (var item in mixedItems)
                {
                    NewsItems.Add(item);
                }
                _currentOffset = pagedItems.Count;
                CanLoadMore = _fullListCache.Count > _currentOffset;
                IsContentEmpty = NewsItems.Count == 0;
                
                // Si no hay nada tras filtrar, marcamos resultados no encontrados
                NoResultsFound = !IsContentEmpty && pagedItems.Count == 0;
            });
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error cargando noticias por fuente: {ex.Message}");

            HasError = true;
            ErrorMessage = "No se pudieron cargar las noticias. Comprueba tu conexión e inténtalo de nuevo.";

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

    private string GetFriendlyName(string id)
    {
        return id.ToLower() switch
        {
            "elpais" => "EL País",
            "elmundo" => "El Mundo",
            "lavanguardia" => "La Vanguardia",
            "elperiodico" => "El Periódico",
            "20minutos" => "20 Minutos",
            "elconfidencial" => "El Confidencial",
            "eldiarioes" => "ElDiario.es",
            "bbci" or "bbc" => "BBC Mundo",
            "xataka" => "Xataka",
            "applesfera" => "Applesfera",
            "microsiervos" => "Microsiervos",
            "hipertextual" => "Hipertextual",
            "vidaextra" => "VidaExtra",
            "espinof" => "Espinof",
            "3djuegos" => "3DJuegos",
            "hobbyconsolas" => "Hobby Consolas",
            "ign" => "IGN España",
            "eltiempo" or "eltiempoes" => "El Tiempo.es",
            "efeverde" => "EFE VERDE",
            "esa" => "ESA",
            "ecoticias" => "Ecoticias",
            "astroaficion" => "Astroafición",
            "fronteraespacial" => "Frontera Espacial",
            "eurogamer" => "Eurogamer",
            _ => "No se encuentra la fuente.."
        };
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
}

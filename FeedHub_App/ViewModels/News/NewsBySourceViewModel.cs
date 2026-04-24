using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FeedHub_Core.Services;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using FeedHub_App.Views.News;
using CommunityToolkit.Mvvm.Input;

namespace FeedHub_App.ViewModels.News;

[QueryProperty(nameof(SourceId), "source")]
public partial class NewsBySourceViewModel : ObservableObject
{
    private readonly INewsAggregatorService _newsService;
    private readonly ILogger _logger;

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
    
    public ObservableCollection<NewsItem> NewsItems{get; set;} = new();
    public NewsBySourceViewModel(INewsAggregatorService newsService)
    {
        _newsService = newsService;
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
        if (IsLoading) return;

        try
        {
            if (!IsRefreshing) IsLoading = true;
            
            NoResultsFound = false;
            _currentOffset = 0;

            // Obtenemos los datos
            var allItems = await _newsService.GetBySourceAsync(SourceId, 100); 
            
            // Filtramos la primera página
            var items = allItems.Take(PageSize).ToList();

            NewsItems.Clear();
            foreach (var item in items) 
                NewsItems.Add(item);

            // Estados finales
            CanLoadMore = allItems.Count > PageSize;
            NoResultsFound = NewsItems.Count == 0;
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"Error cargando noticias: {ex.Message}");
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
        // Si ya está cargando o no hay más, salimos
        if (IsLoading || IsLoadingMore || !CanLoadMore) return;

        try
        {
            IsLoadingMore = true; // Activamos estado específico para el footer
            _currentOffset += PageSize;

            var allItems = await _newsService.GetBySourceAsync(SourceId, 100); 
            
            // Simular un pequeño delay para que la transición no sea brusca (opcional)
            await Task.Delay(300);

            var existingLinks = NewsItems.Select(n => n.Link).ToHashSet();
            var newItems = allItems.Skip(_currentOffset)
                                   .Take(PageSize)
                                   .Where(n => !existingLinks.Contains(n.Link))
                                   .ToList();

            if (newItems.Any())
            {
                foreach (var item in newItems) 
                    NewsItems.Add(item);
                
                CanLoadMore = allItems.Count > NewsItems.Count;
            }
            else
            {
                CanLoadMore = false;
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"Error cargando más: {ex.Message}");
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
}

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
        System.Diagnostics.Debug.WriteLine($"*** CARGANDO FUENTE: {SourceId} ***");

        if (NewsItems.Count > 0 && !IsRefreshing) return;

        try
        {
            if (!IsRefreshing)
                IsLoading = true;

            var items = await _newsService.GetBySourceAsync(SourceId); 

            NewsItems.Clear();
            foreach (var item in items) 
                NewsItems.Add(item);
        }
        catch (Exception ex) 
        { 
            _logger?.Error(ex.Message); 
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
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
            "econonoticias" => "Econoticias",
            "astroaficion" => "Astroafición",
            "fronteraespacial" => "Frontera Espacial",
            "eurogamer" => "Eurogamer",
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

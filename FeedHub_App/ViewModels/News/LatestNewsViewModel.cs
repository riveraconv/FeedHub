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
        private ObservableCollection<NewsItem>_news = new();
        public ObservableCollection<NewsItem> News
        {
            get => _news;
            set
            {
                if(_news != value)
                {
                    //optimiza que la pantalla solo se dibuje de una
                    _news = value;
                    OnPropertyChanged(nameof(News));
                }
            }
        }

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
    // 1. Si ya hay noticias y no estamos refrescando, no hacemos nada
    if (News.Count > 0 && !IsRefreshing) return;

    try
    {
        // 2. Control de los indicadores de carga
        if (!IsRefreshing)
            IsLoading = true;

        // 3. Traemos las noticias (Esto se hace en un hilo secundario, ¡bien!)
        var selected = await _aggregatorService.GetLatestMixedAsync(30);

        // 4. EL CAMBIO CLAVE: 
        // En lugar de hacer .Clear() y luego un bucle .Add() que congela la UI 30 veces,
        // creamos la colección de golpe en el hilo principal.
        MainThread.BeginInvokeOnMainThread(() => 
        {
            // Reemplazamos la instancia completa. 
            // Esto dispara UN SOLO evento de actualización a la pantalla.
            News = new ObservableCollection<NewsItem>(selected);
        });
    }
    catch (Exception ex) 
    { 
        _logger?.Error($"Error cargando noticias: {ex.Message}"); 
    }
    finally
    {
        // 5. Limpiamos estados de carga
        IsLoading = false;
        IsRefreshing = false; 
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


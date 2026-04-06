using FeedHub_Core.Models;
using FeedHub_Core.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Utilities;
using System.ComponentModel;

namespace FeedHub_App.ViewModels.News
{
    public partial class CategoryListViewModel : ObservableObject, IQueryAttributable
    {
        private readonly INewsAggregatorService _aggregator;
        private readonly ILogger _logger;

        public ObservableCollection<NewsItem> Articles { get; } = new();
        public ObservableCollection<string> Categories { get; } = new()
        {
            "Internacional",
            "Política",
            "Economía",
            "Tecnología",
            "Ciencia",
            "Deportes",
            "Sociedad",
            "Cultura",
            "Entretenimiento",
        };

        [ObservableProperty]
        public string category;

        [ObservableProperty]
        public bool isLoading;

        [ObservableProperty]
        public bool isRefreshing;

        [ObservableProperty]
        private string searchText;

        public CategoryListViewModel(INewsAggregatorService aggregator, ILogger logger)
        {
            _aggregator = aggregator;
            _logger = logger;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("category"))
            {
                var newCategory = Uri.UnescapeDataString(query["category"]?.ToString() ?? "");

                // SOLO cargamos si la categoría ha cambiado o si la lista está vacía
                if (Category != newCategory || Articles.Count == 0)
                {
                    Category = newCategory;
                    // Ejecutamos la carga inicial
                    LoadNewsCommand.Execute(null);
                }
            }
            else if (query.ContainsKey("search"))
            {
                var queryText = Uri.UnescapeDataString(query["search"]?.ToString() ?? "");
                Category = $"Search: {queryText}";

                PerformSearch(queryText);
            }
        }
        private async void PerformSearch(string queryText)
        {
            IsLoading = true;
            try
            {
                var results = await _aggregator.SearchByKeywordAsync(queryText, 50);

                Articles.Clear();
                foreach (var item in results)
                    Articles.Add(item);
            }
            catch (Exception ex) { _logger.Error(ex.Message); }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            // Lógica espejo de LatestNewsViewModel: 
            // Si ya hay artículos y NO es un "Pull to Refresh", no hacemos nada.
            if (Articles.Count > 0 && !IsRefreshing) return;

            if (string.IsNullOrWhiteSpace(Category) || IsLoading) return;

            try
            {
                if (!IsRefreshing) IsLoading = true;

                var items = await _aggregator.GetByCategoryAsync(Category, 100);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Articles.Clear();

                    var filtered = items
                        .GroupBy(i => i.Source)
                        .SelectMany(g => g.Take(4)) // No más de 4 seguidas del mismo
                        .OrderByDescending(i => i.PublishDate)
                        .Take(100);

                    foreach (var item in filtered)
                        Articles.Add(item);
                });
            }
            catch (Exception ex)
            {
                _logger?.Error($"Categories Loading Error {Category}: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task OpenNewsAsync(NewsItem item)
        {
            if (item == null) return;

            await Shell.Current.GoToAsync(
                $"QuickViewPage?link={Uri.EscapeDataString(item.Link)}" +
                $"&title={Uri.EscapeDataString(item.Title)}" +
                $"&imageUrl={Uri.EscapeDataString(item.ImageUrl ?? string.Empty)}" +
                $"&source={Uri.EscapeDataString(item.Source ?? "Fuente")}");
        }
        [RelayCommand]
        public async Task SelectCategoryAsync(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return;

            // Navegamos a la página de noticias filtradas que ya creamos
            // Pasamos el nombre de la categoría por la URL (QueryParameters)
            await Shell.Current.GoToAsync($"CategoryNewsPage?category={Uri.EscapeDataString(categoryName)}");
        }
        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            var query = SearchText;

            // Navegamos primero
            await Shell.Current.GoToAsync($"CategoryNewsPage?search={Uri.EscapeDataString(query)}");

            // Limpiamos después de una pequeña pausa para que no interfiera con la animación
            await Task.Delay(500);
            SearchText = string.Empty;
        }

    }
}


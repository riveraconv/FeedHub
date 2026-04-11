using FeedHub_Core.Models;
using FeedHub_Core.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Utilities;


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
        [ObservableProperty]
        string pageTitle;
        [ObservableProperty]
        bool noResultsFound;
        [ObservableProperty]
        bool isSearchMode;
        public event Action? SearchCompleted;

        public CategoryListViewModel(INewsAggregatorService aggregator, ILogger logger)
        {
            _aggregator = aggregator;
            _logger = logger;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("category"))
            {
                IsSearchMode = false;
                var newCategory = Uri.UnescapeDataString(query["category"]?.ToString() ?? "");

                // SOLO cargamos si la categoría ha cambiado o si la lista está vacía
                if (Category != newCategory || Articles.Count == 0)
                {
                    Category = newCategory;
                    PageTitle = Category;
                    LoadNewsCommand.Execute(null);
                }
            }
            else if (query.ContainsKey("search"))
            {
                var queryText = Uri.UnescapeDataString(query["search"]?.ToString() ?? "");
                Category = queryText;
                PageTitle = $"Resultados de '{queryText}'";
                PerformSearch(queryText);
            }
        }
        private async void PerformSearch(string queryText)
        {
            isSearchMode = true;
            IsLoading = true;
            NoResultsFound = false;
            Articles.Clear();
            try
            {
                var results = await Task.Run(() => _aggregator.SearchByKeywordAsync(queryText, 50));
                if(results != null & results.Any())
                {
                    foreach (var item in results)
                    Articles.Add(item);
                }
                NoResultsFound = Articles.Count == 0;
            }
            catch (Exception ex) { _logger.Error(ex.Message); }
            finally 
            { 
                IsLoading = false; 
                SearchCompleted?.Invoke();
            }
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
        [RelayCommand]
        public async Task SearchInPageAsync()
        {
            if(string.IsNullOrWhiteSpace(SearchText)) return;

            var query = SearchText;
            SearchText = string.Empty;
            PageTitle = $"Resultados de '{query}'";
            PerformSearch(query);
        }

    }
}


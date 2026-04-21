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
        private string category = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string pageTitle = string.Empty;

        [ObservableProperty]
        private bool noResultsFound;

        [ObservableProperty]
        private bool isSearchMode;

        public event Action? SearchCompleted;

        private string? _initialQuery;
        private int _currentOffset = 0;
        private const int PageSize = 20;

        [ObservableProperty]
        private bool canLoadMore;

        [ObservableProperty]
        private string dynamicPlaceholder = string.Empty;

        private int _actualIndex = 0;

        private readonly string[] _sugestions =
        {
        "No encuentras alguna noticia...?",
        "Prueba a buscar por palabras!"
    };

        [ObservableProperty]
        private bool isLoadingMore;

        // 🔥 ESTE ES EL CAMBIO IMPORTANTE
        [ObservableProperty]
        private CategoryViewState viewState;
        private List<NewsItem> _fullListCache = new();


        public CategoryListViewModel(INewsAggregatorService aggregator, ILogger logger)
        {
            _aggregator = aggregator;
            _logger = logger;
            StartPlaceholderAnimation();
        }
        public void StartPlaceholderAnimation()
        {
            DynamicPlaceholder = _sugestions[0];

            IDispatcherTimer timer = Application.Current.Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) =>
            {
                _actualIndex = (_actualIndex + 1) % _sugestions.Length;
                DynamicPlaceholder = _sugestions[_actualIndex];
            };
            timer.Start();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("category"))
            {
                IsSearchMode = false;
                _initialQuery = null;
                var newCategory = Uri.UnescapeDataString(query["category"]?.ToString() ?? "");

                if (Category != newCategory || Articles.Count == 0)
                {
                    _currentOffset = 0;
                    Category = newCategory;
                    PageTitle = Category;
                    LoadNewsCommand.Execute(null);
                }
            }
            else if (query.ContainsKey("search"))
            {
                var queryText = Uri.UnescapeDataString(query["search"]?.ToString() ?? "");

                // Si es la misma query que ya tenemos cargada, no recargamos
                if (_initialQuery == queryText && Articles.Count > 0) return;

                _initialQuery = queryText;
                Category = queryText;
                PageTitle = $"Resultados de '{queryText}'";
                PerformSearch(queryText);
            }
        }
        private async void PerformSearch(string queryText)
        {
            IsSearchMode = true;
            CanLoadMore = false;
            IsLoading = true;
            NoResultsFound = false;
            _currentOffset = 0;

            try
            {
                // 1. Buscamos hasta 100 resultados (o los que quieras de tope)
                var results = await Task.Run(() => _aggregator.SearchByKeywordAsync(queryText, 100));

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _fullListCache = results?.ToList() ?? new List<NewsItem>();
                    Articles.Clear();

                    if (_fullListCache.Any())
                    {
                        // 2. Cargamos el primer bloque de 20
                        var firstBatch = _fullListCache.Take(PageSize).ToList();
                        foreach (var item in firstBatch)
                            Articles.Add(item);

                        _currentOffset = Articles.Count;
                        
                        // 3. Verificamos si podemos cargar más después
                        CanLoadMore = _fullListCache.Count > Articles.Count;
                    }

                    NoResultsFound = Articles.Count == 0;
                    ViewState = NoResultsFound ? CategoryViewState.Empty : CategoryViewState.Content;
                });
            }
            catch (Exception ex) 
            { 
                _logger.Error(ex.Message); 
                ViewState = CategoryViewState.Empty;
            }
            finally
            {
                IsLoading = false;
                SearchCompleted?.Invoke();
            }
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            if (string.IsNullOrWhiteSpace(Category))
                return;

            try
            {
                IsLoading = true;
                IsRefreshing = false;
                IsLoadingMore = false;
                NoResultsFound = false;
                ViewState = CategoryViewState.Loading;

                var items = await _aggregator.GetByCategoryAsync(Category, 100);

                var processed = items
                    .GroupBy(i => i.Source)
                    .SelectMany(g => g.Take(4))
                    .OrderByDescending(i => i.PublishDate)
                    .ToList() ?? new List<NewsItem>();

                    _fullListCache = processed;

                Articles.Clear();

                if (_fullListCache.Any())
                {
                    var firstBatch = _fullListCache.Take(PageSize).ToList();
                    foreach (var item in firstBatch)
                    {
                        Articles.Add(item);
                    }
                    _currentOffset = Articles.Count;
                    CanLoadMore = _fullListCache.Count > Articles.Count;
                    ViewState = CategoryViewState.Content;
                }
                else
                {
                    NoResultsFound = true;
                    ViewState = CategoryViewState.Empty;
                }    
            }
            catch (Exception ex) 
            { 
                _logger.Error(ex.Message); 
                ViewState = CategoryViewState.Empty; // O un estado de Error si lo tienes
            }
            finally
            {
                IsLoading = false;
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
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            var query = SearchText;
            SearchText = string.Empty;
            PageTitle = $"Resultados de '{query}'";
            CanLoadMore = false;
            PerformSearch(query);
        }
        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            // Si ya estamos cargando o no hay más en el caché, salimos
            if (IsLoadingMore || !CanLoadMore) return;

            try
            {
                IsLoadingMore = true;

                // Simulamos un micro-delay (500ms) para que el usuario vea que la app "hace algo"
                // Como los datos ya están en _fullListCache, esto sería instantáneo sin el delay.
                await Task.Delay(500);

                // 1. Obtenemos el siguiente bloque de la lista que ya tenemos descargada
                var nextItems = _fullListCache
                    .Skip(_currentOffset)
                    .Take(PageSize)
                    .ToList();

                // 2. Los añadimos a la lista visible
                foreach (var item in nextItems)
                    Articles.Add(item);

                // 3. Actualizamos el puntero
                _currentOffset = Articles.Count;

                // 4. ¿Quedan más noticias en el caché por mostrar?
                CanLoadMore = _fullListCache.Count > Articles.Count;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }
    }
}


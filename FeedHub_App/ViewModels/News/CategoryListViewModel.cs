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
        private string? _initialQuery;
        private int _currentOffset = 0;
        private const int PageSize = 20;
        [ObservableProperty]
        private bool canLoadMore;

        [ObservableProperty]
        string dynamicPlaceholder; 
        private int _actualIndex = 0;
        private readonly string[] _sugestions =
        {
            "No encuentras alguna noticia...?",
            "Prueba a buscar por palabras!"
        };

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
                var results = await Task.Run(() => _aggregator.SearchByKeywordAsync(queryText, 50));

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Articles.Clear();

                    if(results != null & results.Any())
                    {
                        foreach (var item in results)
                        Articles.Add(item);
                    }
                    NoResultsFound = Articles.Count == 0;
                });
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
                _currentOffset = 0;

                var items = await _aggregator.GetByCategoryAsync(Category, PageSize);

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
                        CanLoadMore = items.Count >= PageSize;
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
            CanLoadMore = false;
            PerformSearch(query);
        }
        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            if(IsLoading || !CanLoadMore || IsSearchMode) return;

            try
            {
                IsLoading = true;
                _currentOffset = Articles.Count;

                var more = await _aggregator.GetByCategoryAsync(Category, PageSize + _currentOffset);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existing = Articles.Select(n => n.Link).ToHashSet();
                    var newItems = more.Skip(_currentOffset)
                                    .Where(n => !existing.Contains(n.Link))
                                    .ToList();

                    foreach (var item in newItems) Articles.Add(item);

                    CanLoadMore = newItems.Count >= PageSize;
                });
            }
            catch (Exception ex) { _logger?.Error(ex.Message); }
            finally { IsLoading = false; }
            }
        }
    }


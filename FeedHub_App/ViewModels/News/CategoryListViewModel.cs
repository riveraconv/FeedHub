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
            "International",
            "Politics",
            "Economy",
            "Technology",
            "Science",
            "Sports",
            "Society",
            "Culture",
            "Climatology",
            "Entertainment",
            
        };

        [ObservableProperty]
        public string category;

        [ObservableProperty]
        public bool isLoading;

        [ObservableProperty]
        public bool isRefreshing;

        public CategoryListViewModel(INewsAggregatorService aggregator, ILogger logger)
        {
            _aggregator = aggregator;
            _logger = logger;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("category"))
            {
                Category = Uri.UnescapeDataString(query["category"]?.ToString() ?? "");
                // Ejecutamos el comando sin pasarle nada, él leerá la propiedad 'Category'
                LoadNewsCommand.Execute(null);
            }
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            if (string.IsNullOrWhiteSpace(Category) || IsLoading) return;

            try
            {
                if (!IsRefreshing) IsLoading = true;
            
            var items = await _aggregator.GetByCategoryAsync(Category, 30);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Articles.Clear();
                    foreach (var item in items)
                        Articles.Add(item);
                });
            }
            catch (Exception ex)
            {
                _logger?.Error($"Categoies Loading Error {Category}: {ex.Message}");
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
                $"&imageUrl={Uri.EscapeDataString(item.ImageUrl ?? string.Empty)}");
        }
        [RelayCommand]
        public async Task SelectCategoryAsync(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return;

            // Navegamos a la página de noticias filtradas que ya creamos
            // Pasamos el nombre de la categoría por la URL (QueryParameters)
            await Shell.Current.GoToAsync($"CategoryNewsPage?category={Uri.EscapeDataString(categoryName)}");
        }
    }
}


using FeedHub_Core.Models;
using FeedHub_Core.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Dispatching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FeedHub_App.ViewModels.News
{
    public partial class CategoryNewsViewModel : ObservableObject, IQueryAttributable
    {
        private readonly INewsAggregatorService _aggregator;

        public ObservableCollection<NewsItem> Articles { get; } = new ObservableCollection<NewsItem>();
        [ObservableProperty]
        public string category;

        public CategoryNewsViewModel(INewsAggregatorService aggregator)
        {
            _aggregator = aggregator;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("category"))
            {
                Category = query["category"]?.ToString();
            }
        }

        [RelayCommand]
        public async Task LoadNewsAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            var items = await _aggregator.GetByCategoryAsync(category, 30);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Articles.Clear();
                foreach (var item in items)
                    Articles.Add(item);
            });
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
    }
}



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using FeedHub_App.Views.News;

namespace FeedHub_App.ViewModels.News
{
    public partial class LatestNewsViewModel : ObservableObject
    {
        private readonly IRssService _rssService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<NewsItem> news = new();
        public IAsyncRelayCommand LoadNewsCommand { get; }

        public LatestNewsViewModel(IRssService rssService)
        {
            _rssService = rssService;
            LoadNewsCommand = new AsyncRelayCommand(LoadNewsAsync);
        }

        private readonly Dictionary<string, string> _rssFeeds = new()
        {
            //El Pais
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/sociedad/portada", "society"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/internacional/portada", "international" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/economia/portada" ,"economy"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/ciencia/portada ", "cience" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/tecnologia/portada", "tecnology"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/cultura/portada", "culture" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/deportes/portada", "sports" },
            {"https://feeds.elpais.com/mrss-s/list/ep/site/elpais.com/section/clima-y-medio-ambiente", "climatology"}
        };
        public async Task LoadNewsAsync()
        {
            Debug.WriteLine("LoadNewsAsync started");

            if (IsLoading) return;

            try
            {
                IsLoading = true;
                var allItems = new List<NewsItem>();

                var results = await Task.Run(async () =>
                {
                    var tempList = new List<NewsItem>();
                    var tasks = _rssFeeds.Select(async kvp =>
                    {
                        var feedUrl = kvp.Key;
                        var category = kvp.Value;

                        try
                        {
                            var items = await _rssService.GetNewsAsync(feedUrl);
                            var latest = items
                                .OrderByDescending(i => i.PublishDate)
                                .FirstOrDefault();

                            if (latest != null)
                            {
                                latest.Category = category;
                                lock (tempList)
                                    tempList.Add(latest);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            Debug.WriteLine($"Error loading feed {feedUrl}: {ex.Message}");

                            lock (tempList)
                            {
                                tempList.Add(new NewsItem
                                {
                                    Title = "⚠️ Feed might be broken or URL changed",
                                    Description = ex.Message,
                                    Source = feedUrl,
                                    PublishDate = DateTime.Now,
                                    Category = category
                                });
                            }
                        }
                    });

                    await Task.WhenAll(tasks);
                    return tempList;
                });

                // Seleccionar aleatoriamente algunas noticias
                var random = new Random();
                var selected = results
                    .Where(item => item != null)
                    .OrderBy(x => random.Next())
                    .Take(7)
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    News.Clear();
                    foreach (var item in selected)
                        News.Add(item);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }
        [RelayCommand]
        private async Task OpenNewsAsync(NewsItem item)
        {
            if (item == null) return;

            await Shell.Current.GoToAsync($"QuickViewPage?link={Uri.EscapeDataString(item.Link)}" +
                                          $"&title={Uri.EscapeDataString(item.Title)}" +
                                          $"&imageUrl={Uri.EscapeDataString(item.ImageUrl ?? string.Empty)}");
        }
    }
}

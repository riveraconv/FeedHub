
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;

namespace FeedHub_App.ViewModels.News
{
    public partial class LatestNewsViewModel : ObservableObject
    {
        private readonly IRssService _rssService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<NewsItem> news;

        public LatestNewsViewModel(IRssService rssService)
        {
            _rssService = rssService;
            News = new ObservableCollection<NewsItem>();
            LoadNewsCommand = new AsyncRelayCommand(LoadNewsAsync);
        }
        public IAsyncRelayCommand LoadNewsCommand { get; }

        private readonly Dictionary<string, string> _rssFeeds = new()
        {
            //El Pais
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/sociedad/portada", "society"},
            { "https://e00-elmundo.uecdn.es/rss/politica.xml", "Política" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/internacional/portada", "international" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/economia/portada" ,"economy"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/ciencia/portada ", "cience" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/tecnologia/portada", "tecnology"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/cultura/portada", "culture" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/deportes/portada", "sports" },
            {"https://feeds.elpais.com/mrss-s/list/ep/site/elpais.com/section/clima-y-medio-ambiente", "climatology"}
        };
        private async Task LoadNewsAsync()
        {
            try
            {
                IsLoading = true;
                var allItems = new List<NewsItem>();

                foreach (var kvp in _rssFeeds)
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
                            allItems.Add(latest);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Debug.WriteLine($"Error on load the feed {feedUrl}: {ex.Message}");

                        allItems.Add(new NewsItem
                        {
                            Title = "⚠️ Maybe the feed is broken or url was modified by the owner",
                            Description = ex.Message,
                            Source = feedUrl,
                            PublishDate = DateTime.Now,
                            Category = category
                        });
                    }
                }

                var random = new Random();
                var shuffled = allItems.OrderBy(x => random.Next()).ToList();
                var selected = shuffled.Take(7);

                News.Clear();
                foreach (var item in selected)
                    News.Add(item);
            }
            finally
            {
                IsLoading = false;
            }
        }
        [RelayCommand]
        private async Task OpenNewsAsync(NewsItem news)
        {
            if (news == null || string.IsNullOrWhiteSpace(news.Link))
                return;

            try
            {
                await Launcher.OpenAsync(new Uri(news.Link));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening link: {ex.Message}");
            }
        }
    }
}

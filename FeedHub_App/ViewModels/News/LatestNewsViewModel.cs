
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using System.Collections.ObjectModel;
using FeedHub_Core.Utilities;


namespace FeedHub_App.ViewModels.News
{
    public partial class LatestNewsViewModel : ObservableObject
    {
        private readonly IRssService _rssService;
        private readonly ILogger _logger;

        [ObservableProperty]
        private ObservableCollection<NewsItem> news = new();

        [ObservableProperty]
        private bool isRefreshing = false;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isInitialLoadComplete = false;

        public IAsyncRelayCommand LoadNewsCommand { get; }

        public LatestNewsViewModel(IRssService rssService, ILogger logger)
        {
            _rssService = rssService;
            _logger = logger;

            LoadNewsCommand = new AsyncRelayCommand(LoadNewsAsync);
            _logger.Info("Initialized");
        }

        private readonly Dictionary<string, string> _rssFeeds = new()
        {
            //El Pais
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/sociedad/portada", "society"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/internacional/portada", "international" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/economia/portada" ,"economy"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/ciencia/portada ", "science" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/tecnologia/portada", "technology"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/cultura/portada", "culture" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/deportes/portada", "sports" },
            {"https://feeds.elpais.com/mrss-s/list/ep/site/elpais.com/section/clima-y-medio-ambiente", "climatology"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/espana/portada", "Spain" },

            //El Mundo
            {"https://e00-elmundo.uecdn.es/elmundo/rss/internacional.xml", "international"},
            {"https://e00-elmundo.uecdn.es/elmundo/rss/economia.xml", "economy"},
            {"https://e00-elmundo.uecdn.es/elmundo/rss/cultura.xml", "culture" },
            {"https://e00-elmundo.uecdn.es/elmundodeporte/rss/portada.xml", "sports" },
            {"https://e00-elmundo.uecdn.es/elmundo/rss/espana.xml", "Spain" },

            //La Vanguardia
            {"https://www.lavanguardia.com/rss/internacional.xml", "international"},
            {"https://www.lavanguardia.com/rss/politica.xml", "politics" },
            {"https://www.lavanguardia.com/rss/deportes.xml", "sports" },
            {"https://www.lavanguardia.com/rss/economia.xml", "economy" },
            {"https://www.lavanguardia.com/rss/cultura.xml", "culture" },
            {"https://www.lavanguardia.com/rss/natural.xml", "science, ecology" },
            
            //El Periodico
            {"https://www.elperiodico.com/es/rss/internacional/rss.xml", "international" },
            {"https://www.elperiodico.com/es/rss/politica/rss.xml", "politics" },
            {"https://www.elperiodico.com/es/rss/economia/rss.xml", "economy" },
            {"https://www.elperiodico.com/es/rss/tecnologia/rss.xml", "technology" },
            {"https://www.elperiodico.com/es/rss/sociedad/rss.xml", "society" },
            {"https://www.elperiodico.com/es/rss/ciencia/rss.xml", "science" },
            {"https://www.elperiodico.com/es/rss/deportes/rss.xml", "sports" },
            {"https://www.elperiodico.com/es/rss/ocio-y-cultura/rss.xml", "culture" },

            //20 Minutos
            {"https://www.20minutos.es/rss/internacional/", "international" },
            {"https://www.20minutos.es/rss/deportes/", "sports" },
            {"https://www.20minutos.es/rss/economia", "economy" },
            {"https://www.20minutos.es/rss/tecnologia/", "technology" },
            {"https://www.20minutos.es/rss/salud/", "health" },

            //El Confidencial
            {"https://rss.elconfidencial.com/espana/", "Spain" },
            {"https://rss.elconfidencial.com/mundo/", "international" },
            {"https://rss.elconfidencial.com/economia/", "econonmy" },
            {"https://rss.elconfidencial.com/deportes/", "sports" },
            {"https://rss.elconfidencial.com/cultura/", "culture" },
            {"https://rss.elconfidencial.com/tecnologia/", "technology" },

            //eldiario.es
            {"https://www.eldiario.es/rss/politica", "politics" },
            {"http://www.eldiario.es/rss/economia ", "economy" },
            {"http://www.eldiario.es/rss/cultura", "culture" },
            {"https://www.eldiario.es/rss/internacional/", "international"},
            {"https://www.eldiario.es/rss/economia/", "economy" },
            {"https://www.eldiario.es/rss/focos/crisis-climatica/", "climatology" },
            {"https://www.eldiario.es/rss/tecnologia/", "tecnology" },
        };

        public async Task LoadNewsAsync()
        {
            IsRefreshing = true;
            if (!IsInitialLoadComplete)
            {
                IsLoading = true;
            }
            try
            {
                _logger.Info("Started");

                await Task.Run(async () =>
                {
                    var tempList = new List<NewsItem>();
                    var tasks = _rssFeeds.Select(async kvp =>
                    {
                        var feedUrl = kvp.Key;
                        var category = kvp.Value;

                        try
                        {
                            var items = await _rssService.GetNewsAsync(feedUrl);
                            var latest = items.OrderByDescending(i => i.PublishDate).FirstOrDefault();
                            if (latest != null)
                            {
                                latest.Category = category;
                                lock (tempList) tempList.Add(latest);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.Warn($"Error loading the feed {feedUrl}: {ex.Message}");

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

                    var random = new Random();
                    var selected = tempList
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
                });

                _logger.Info("Finished");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unhandled error during LoadNewAync:{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                IsInitialLoadComplete = true;

            }
        }

        [RelayCommand]
        private async Task OpenNewsAsync(NewsItem item)
        {
            if (item == null) return;

            await Shell.Current.GoToAsync($"QuickViewPage?link={Uri.EscapeDataString(item.Link)}" +
                                          $"&title={Uri.EscapeDataString(item.Title)}" +
                                          $"&imageUrl={Uri.EscapeDataString(item.ImageUrl ?? string.Empty)}");

            _logger.Info("Worked");
        }
    }
}

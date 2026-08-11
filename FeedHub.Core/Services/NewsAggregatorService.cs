
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using System.Collections.Concurrent;
using System.Globalization;

namespace FeedHub_Core.Services;


    public class NewsAggregatorService : INewsAggregatorService
    {
        private readonly IRssService _rssService;
        private readonly FilterPreferencesService _filterService;
        private readonly ILogger _logger;
        private readonly IReadOnlyList<FeedSourceConfig> _catalogSources;

        public List<string> GetAvailableCategories() => 
                            _catalogSources
                                .SelectMany(source => source.Feeds)
                                .Select(feed => feed.Category)
                                .Distinct()
                                .OrderBy(category => category)
                                .ToList();

        public List<string> GetAvailableSources() => 
                            _catalogSources
                                .Select(source => source.Name)
                                .Distinct()
                                .OrderBy(source => source)
                                .ToList();
        public NewsAggregatorService(IRssService rssService, ILogger logger, FilterPreferencesService filterService,
        SourceCatalogService sourceCatalog)
        {
            _rssService = rssService;
            _logger = logger;
            _filterService = filterService;
            _catalogSources = sourceCatalog.GetSources();


        }

        public async Task<List<NewsItem>> GetLatestMixedAsync(int limit)
        {
            var tempList = new ConcurrentBag<NewsItem>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };
            int successfulFeeds = 0;

            var filteredFeeds = _catalogSources
                .SelectMany(source => source.Feeds.Select(feed => new
                {
                    Source = source,
                    Feed = feed
                }))
                .Where(x =>
                _filterService.IsSourceActive(x.Source.Id) &&
                _filterService.IsCategoryActive(x.Feed.Category))
                .ToList();
            

            //el usuario ha filtrado todas las fuentes o categorías
            if(filteredFeeds.Count == 0)
            {
                return new List<NewsItem>();
            }

            int perFeed = filteredFeeds.Count > 0
                ? Math.Max(2, (int)Math.Ceiling((double)limit / filteredFeeds.Count))
                : 2;

            await Parallel.ForEachAsync(filteredFeeds, options, async (x, ct) =>
            {
                try
                {
                    // Timeout individual de 10 seg por feed
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(8));

                    var items = await _rssService.GetNewsAsync(
                        x.Feed.Url,
                        x.Feed.Category,
                        cts.Token);

                    Interlocked.Increment(ref successfulFeeds);

                    var latest = items.OrderByDescending(i => i.PublishDate).Take(perFeed);

                    foreach (var item in latest)
                    {
                        item.Category = x.Feed.Category;
                        item.Source = x.Source.Name;
                        tempList.Add(item);
                    }

                    
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Fallo en feed mixto {x.Source.Id}: {ex.Message}");
                }
            });

            if(successfulFeeds == 0)
                    {
                        throw new HttpRequestException("No se pudo acceder a ningún feed RSS");
                    }
                    
            var result = tempList.OrderByDescending(x => x.PublishDate)
                       .Take(limit)
                       .ToList();

            _logger?.Info($"Feeds OK: {successfulFeeds}/{filteredFeeds.Count}| Noticias obtenidas: {result.Count}");
            return result;
            
        }
    

        public async Task<List<NewsItem>> GetByCategoryAsync(string category, int limit)
        {
            var allItems = new ConcurrentBag<NewsItem>();
            int successfulFeeds = 0;
            DateTime cutOffDate = category == "ciencia" || category == "cultura" || category == "entretenimiento"
                                ? DateTime.Now.AddDays(-2)
                                : DateTime.Now.AddDays(-7);

            var compareInfo = CultureInfo.GetCultureInfo("es-ES").CompareInfo;

            var filteredFeeds = _catalogSources
                .SelectMany(source => source.Feeds.Select(feed => new
                {
                    Source = source,
                    Feed = feed
                }))
                .Where(x =>
                    compareInfo.Compare(
                        x.Feed.Category,
                        category,
                        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0 &&
                        _filterService.IsSourceActive(x.Source.Id)
                    )
                    .ToList();

            var sourceNames = string.Join(", ", 
                filteredFeeds
                    .Select(x => x.Source.Name)
                    .Distinct());

            System.Diagnostics.Debug.WriteLine(
                $"DEBUG Categoría recibida: '{category}' | "+
                $"Feeds encontrados: {filteredFeeds.Count} | "+
                $"Fuentes: {sourceNames}");
            
            await Parallel.ForEachAsync(
                filteredFeeds, 
                new ParallelOptions { MaxDegreeOfParallelism = 15 },
                async (x, ct) => //number of HTTP conections
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var items = await _rssService.GetNewsAsync(
                        x.Feed.Url, 
                        x.Feed.Category,
                        cts.Token);

                    var validItems = items
                        .Where(i => i.PublishDate >= cutOffDate)
                        .ToList();

                    _logger.Info($"{x.Source.Name} -> {validItems.Count} noticias válidas");

                    Interlocked.Increment(ref successfulFeeds);

                    foreach (var item in validItems)
                    {
                        item.Category = x.Feed.Category;
                        item.Source = x.Source.Name;
                        allItems.Add(item);
                    }
                }
                catch(Exception ex)
                {
                    _logger?.Warn($"Error en feed de categoría {x.Feed.Url}: {ex.Message}");
                }
            });
            if (successfulFeeds == 0)
            {
                throw new HttpRequestException("No se pudo cargar ningún feed RSS");
            }

            var result = allItems
                .OrderByDescending(x => x.PublishDate)
                .Take(limit)
                .ToList();

                _logger?.Info(
                $"Categoría '{category}' " +
                $"- Feeds OK: {successfulFeeds}/{filteredFeeds.Count} | "+ 
                $"- Noticias: {result.Count}");

                return result;
        }

        public async Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit = 40)
        {
            if (string.IsNullOrWhiteSpace(query)) 
            return Enumerable.Empty<NewsItem>();

            var allResults = new ConcurrentBag<NewsItem>();
            int successfulFeeds = 0;

            var catalogFeeds = _catalogSources
                .SelectMany(source => source.Feeds.Select(feed => new
                {
                    Source = source,
                    Feed = feed
                }))
                .ToList();

            // Buscamos en todos los feeds en paralelo para encontrar la palabra clave
            await Parallel.ForEachAsync(catalogFeeds, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (x, ct) =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(8));

                    var items = await _rssService.GetNewsAsync(
                        x.Feed.Url,
                        x.Feed.Category,
                        cts.Token);

                    var filtered = items.Where(n =>
                        (n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

                    Interlocked.Increment(ref successfulFeeds);

                    foreach (var item in filtered)
                    {
                        item.Source = x.Source.Name;
                        item.Category = x.Feed.Category;

                        allResults.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Fallo en la búsqueda {x.Feed.Url}: {ex.Message}");
                }
            });

            if(successfulFeeds == 0)
                throw new HttpRequestException("No se pudo acceder a ningún feed RSS");
            
            var result = allResults
                   .OrderByDescending(n => n.PublishDate)
                   .Take(limit)
                   .ToList();

                   _logger.Info($"Búsqueda '{query}' - "+
                   $" Feeds OK: {successfulFeeds}/{catalogFeeds.Count} | "+
                   $" Resultados: {result.Count}");

                   return result;
        }
        public async Task<List<NewsItem>> GetBySourceAsync(string sourceId, int limit = 20)
        {
            var allItems = new ConcurrentBag<NewsItem>();
            int successfulFeeds = 0;

            var source = _catalogSources.FirstOrDefault(source =>
                string.Equals(
                    source.Id,
                    sourceId,
                    StringComparison.OrdinalIgnoreCase));
                
                if (source is null)
                {
                    throw new ArgumentException($"No existe una fuente configurada con el ID '{sourceId}'.",
                    nameof(sourceId));
                }

                var sourceFeeds = source.Feeds
                    .Where(feed => _filterService.IsCategoryActive(feed.Category))
                    .ToList();

            await Parallel.ForEachAsync(sourceFeeds, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (feed, ct) =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(8));

                    var items = await _rssService.GetNewsAsync(
                        feed.Url,
                        feed.Category,
                        cts.Token);

                    Interlocked.Increment(ref successfulFeeds);

                    foreach (var item in items)
                    {
                        item.Source = source.Name;
                        item.Category = feed.Category;

                        allItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warn($"Fallo en feed de fuente {feed.Url}: {ex.Message}");
                }
            });

            if (successfulFeeds == 0)
            throw new HttpRequestException("No se pudo acceder a ningún feed RSS");

            var result = allItems
                .OrderByDescending(x => x.PublishDate)
                .Take(limit)
                .ToList();

                _logger?.Info($"Fuente '{sourceId}' "+
                $" - Feeds OK: {successfulFeeds} / {sourceFeeds.Count} | "+
                $" Noticias: {result.Count}");

                return result;
        }
    }


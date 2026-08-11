
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using System.Collections.Concurrent;
using FeedHub_Core.Helpers;
using System.Globalization;

namespace FeedHub_Core.Services;


    public class NewsAggregatorService : INewsAggregatorService
    {
        private readonly IRssService _rssService;
        private readonly FilterPreferencesService _filterService;
        private readonly ILogger _logger;
        private readonly IReadOnlyList<FeedSourceConfig> _catalogSources;
        private readonly List<KeyValuePair<string, string>> _feeds;

        public List<string> GetAvailableCategories() => 
                            _feeds
                                .Select(feed => feed.Value)
                                .Distinct()
                                .OrderBy(category => category)
                                .ToList();

        public List<string> GetAvailableSources() => 
                            _feeds
                                .Select(feed => SourceNameSolver.Resolve(feed.Key))
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

            _feeds = _catalogSources
                .SelectMany(source => source.Feeds)
                .Select(feed => new KeyValuePair<string, string>(
                    feed.Url,
                    feed.Category))
                .ToList();
        }

        public async Task<List<NewsItem>> GetLatestMixedAsync(int limit)
        {
            var tempList = new ConcurrentBag<NewsItem>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };
            int successfulFeeds = 0;

            var filteredFeeds = _feeds.Where(kvp =>
            
                _filterService.IsSourceActive(SourceNameSolver.Resolve(kvp.Key)) && 
                _filterService.IsCategoryActive(kvp.Value)

            ).ToList();

            //el usuario ha filtrado todas las fuentes o categorías
            if(filteredFeeds.Count == 0)
            {
                return new List<NewsItem>();
            }

            int perFeed = filteredFeeds.Count > 0
                ? Math.Max(2, (int)Math.Ceiling((double)limit / filteredFeeds.Count))
                : 2;

            await Parallel.ForEachAsync(filteredFeeds, options, async (kvp, ct) =>
            {
                try
                {
                    // Timeout individual de 10 seg por feed
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(8));

                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);

                    Interlocked.Increment(ref successfulFeeds);

                    var latest = items.OrderByDescending(i => i.PublishDate).Take(perFeed);

                    foreach (var item in latest)
                    {
                        item.Category = kvp.Value;
                        item.Source = SourceNameSolver.Resolve(item.Link);
                        tempList.Add(item);
                    }

                    
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Fallo en feed mixto {kvp.Key}: {ex.Message}");
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

            var filteredFeeds = _feeds.Where(kvp => compareInfo.Compare(
                kvp.Value,
                category,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0 && 
                _filterService.IsSourceActive(SourceNameSolver.Resolve(kvp.Key))
            ).ToList();

            var sourceNames = string.Join(", ", filteredFeeds
                .Select(feed => SourceNameSolver.Resolve(feed.Key))
                .Distinct());

            System.Diagnostics.Debug.WriteLine(
                $"DEBUG Categoría recibida: '{category}' | Feeds encontrados: {filteredFeeds.Count} | Fuentes: {sourceNames}");
            
            await Parallel.ForEachAsync(filteredFeeds, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (kvp, ct) => //number of HTTP conections
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);

                    var validItems = items
                        .Where(i => i.PublishDate >= cutOffDate)
                        .ToList();

                    _logger.Info($"{SourceNameSolver.Resolve(kvp.Key)} -> {validItems.Count} noticias válidas");

                    Interlocked.Increment(ref successfulFeeds);

                    foreach (var item in validItems)
                    {
                        item.Category = kvp.Value;
                        item.Source = SourceNameSolver.Resolve(item.Link);
                        allItems.Add(item);
                    }
                }
                catch(Exception ex)
                {
                    _logger?.Warn($"Error en feed de categoría {kvp.Key}: {ex.Message}");
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
                $"Categoría '{category}' - Feeds OK: {successfulFeeds}/{filteredFeeds.Count} | Noticias: {result.Count}");

                return result;
        }

        public async Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit = 40)
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<NewsItem>();

            var allResults = new ConcurrentBag<NewsItem>();
            int successfulFeeds = 0;

            // Buscamos en todos los feeds en paralelo para encontrar la palabra clave
            await Parallel.ForEachAsync(_feeds, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (kvp, ct) =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(8));

                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);
                    var filtered = items.Where(n =>
                        (n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

                    Interlocked.Increment(ref successfulFeeds);

                    foreach (var item in filtered)
                    {
                        item.Source = SourceNameSolver.Resolve(item.Link);
                        allResults.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Fallo en la búsqueda {kvp.Key}: {ex.Message}");
                }
            });

            if(successfulFeeds == 0)
                throw new HttpRequestException("No se pudo acceder a ningún feed RSS");
            
            var result = allResults
                   .OrderByDescending(n => n.PublishDate)
                   .Take(limit)
                   .ToList();

                   _logger.Info($"Búsqueda '{query}' - Feeds OK: {successfulFeeds}/{_feeds.Count} | Resultados: {result.Count}");

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
                    .Select(feed => new KeyValuePair<string, string>(
                        feed.Url,
                        feed.Category))
                    .ToList();

            await Parallel.ForEachAsync(sourceFeeds, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (kvp, ct) =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(8));

                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);

                    Interlocked.Increment(ref successfulFeeds);

                    foreach (var item in items)
                    {
                        item.Source = source.Name;
                        allItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warn($"Fallo en feed de fuente {kvp.Key}: {ex.Message}");
                }
            });

            if (successfulFeeds == 0)
            throw new HttpRequestException("No se pudo acceder a ningún feed RSS");

            var result = allItems
                .OrderByDescending(x => x.PublishDate)
                .Take(limit)
                .ToList();

                _logger?.Info($"Fuente '{sourceId}' - Feeds OK: {successfulFeeds} / {sourceFeeds.Count} | Noticias: {result.Count}");

                return result;
        }
    }


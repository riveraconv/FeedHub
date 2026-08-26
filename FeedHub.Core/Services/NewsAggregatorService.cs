
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

        public async Task<NewsQueryResult> GetLatestMixedAsync(int limit)
        {
            var tempList = new ConcurrentBag<NewsItem>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };
            int successfulFeeds = 0;

            var allFeeds = _catalogSources
                .SelectMany(source => source.Feeds.Select(feed => new
                {
                    Source = source,
                    Feed = feed
                }))
                .ToList();

            // Feeds cuya categoría está activa.
            // Todavía no tenemos en cuenta el filtro de fuentes.
            var categoryEnabledFeeds = allFeeds
                .Where(x => _filterService.IsCategoryActive(x.Feed.Category))
                .ToList();

            // Feeds que además pertenecen a una fuente activa.
            var filteredFeeds = categoryEnabledFeeds
                .Where(x => _filterService.IsSourceActive(x.Source.Id))
                .ToList();

                //DEBUG ------------------------------>

                var activeSources = filteredFeeds
                    .Select(x => x.Source.Name)
                    .Distinct()
                    .ToList();

                _logger?.Info(
                    $"FUENTES ACTIVAS EN LATEST NEWS: {string.Join(", ", activeSources)}");

                foreach (var source in _catalogSources)
                {
                    _logger?.Info(
                        $"FILTRO FUENTE -> {source.Name} ({source.Id}) = " +
                        $"{_filterService.IsSourceActive(source.Id)}");
                }

                // <-------------------------------------

            var hasActiveSources = _catalogSources
                .Any(source => _filterService.IsSourceActive(source.Id));

            var hasActiveCategories = allFeeds
                .Any(x => _filterService.IsCategoryActive(x.Feed.Category));

                // No hay ninguna fuente o categoría activa.
            // El usuario ha filtrado todo el contenido.
            if (!hasActiveSources || !hasActiveCategories)
            {
                _logger?.Info(
                    "Latest News: el usuario ha filtrado todo el contenido.");

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.FilteredOut,
                    Items = new List<NewsItem>()
                };
            }

            //el usuario ha filtrado todas las fuentes o categorías
            if (filteredFeeds.Count == 0)
            {
                var activeSourceIds = _catalogSources
                    .Where(source => _filterService.IsSourceActive(source.Id))
                    .Select(source => source.Id)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var feedsFromActiveSources = allFeeds
                    .Where(x => activeSourceIds.Contains(x.Source.Id))
                    .ToList();

                // No existe ningún feed que combine una categoría activa
                // con una fuente activa.
                if (!feedsFromActiveSources.Any(x =>
                        _filterService.IsCategoryActive(x.Feed.Category)))
                {
                    _logger?.Info(
                        "Latest News: ninguna fuente activa dispone de feeds " +
                        "para las categorías activas.");

                    return new NewsQueryResult
                    {
                        Status = NewsQueryStatus.NoContent,
                        Items = new List<NewsItem>()
                    };
                }

                // Sí existe contenido potencial para la combinación de filtros,
                // pero ha sido eliminado por los filtros.
                _logger?.Info(
                    "Latest News: el contenido disponible ha sido filtrado.");

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.FilteredOut,
                    Items = new List<NewsItem>()
                };
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
                    
           var result = tempList
                .OrderByDescending(x => x.PublishDate)
                .Take(limit)
                .ToList();

            _logger?.Info(
                $"Feeds OK: {successfulFeeds}/{filteredFeeds.Count} | " +
                $"Noticias obtenidas: {result.Count}");

            if (result.Count == 0)
            {
                _logger?.Info(
                    "Latest News: los feeds activos funcionan, pero no hay noticias disponibles.");

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.NoContent,
                    Items = result
                };
            }

            return new NewsQueryResult
            {
                Status = NewsQueryStatus.Success,
                Items = result
            };     
        }
    
        public async Task<NewsQueryResult> GetByCategoryAsync(string category, int limit)
        {
            var allItems = new ConcurrentBag<NewsItem>();
            int successfulFeeds = 0;

            DateTime cutOffDate =
                category == "ciencia" ||
                category == "cultura" ||
                category == "entretenimiento"
                    ? DateTime.Now.AddDays(-2)
                    : DateTime.Now.AddDays(-7);

            var compareInfo = CultureInfo.GetCultureInfo("es-ES").CompareInfo;

            // 1. Todos los feeds que realmente pertenecen a esta categoría
            var categoryFeeds = _catalogSources
                .SelectMany(source => source.Feeds.Select(feed => new
                {
                    Source = source,
                    Feed = feed
                }))
                .Where(x =>
                    compareInfo.Compare(
                        x.Feed.Category,
                        category,
                        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0)
                .ToList();

            // No existe ningún feed configurado para esta categoría
            if (categoryFeeds.Count == 0)
            {
                _logger?.Info(
                    $"Categoría '{category}' sin feeds configurados.");

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.NoFeedsConfigured
                };
            }

            // 2. De esos feeds, nos quedamos únicamente con las fuentes activas
            var filteredFeeds = categoryFeeds
    .Where(x => _filterService.IsSourceActive(x.Source.Id))
    .ToList();

_logger?.Info(
    $"[DEBUG CATEGORY FILTER] Categoría '{category}'");

foreach (var feed in categoryFeeds)
{
    _logger?.Info(
        $"[DEBUG CATEGORY FILTER] " +
        $"Fuente: {feed.Source.Name} | " +
        $"Id: {feed.Source.Id} | " +
        $"Activa: {_filterService.IsSourceActive(feed.Source.Id)} | " +
        $"Feed: {feed.Feed.Url}");
}

            // La categoría existe, pero todas sus fuentes están filtradas
            if (filteredFeeds.Count == 0)
            {
                _logger?.Info(
                    $"Categoría '{category}' tiene {categoryFeeds.Count} feeds, " +
                    $"pero todos pertenecen a fuentes filtradas.");

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.FilteredOut
                };
            }

            var sourceNames = string.Join(
                ", ",
                filteredFeeds
                    .Select(x => x.Source.Name)
                    .Distinct());

            _logger?.Info(
                $"Categoría '{category}' | " +
                $"Feeds configurados: {categoryFeeds.Count} | " +
                $"Feeds activos: {filteredFeeds.Count} | " +
                $"Fuentes: {sourceNames}");

            // 3. Descargamos únicamente los feeds de fuentes activas
            await Parallel.ForEachAsync(
                filteredFeeds,
                new ParallelOptions { MaxDegreeOfParallelism = 15 },
                async (x, ct) =>
                {
                    try
                    {
                        using var cts =
                            CancellationTokenSource.CreateLinkedTokenSource(ct);

                        cts.CancelAfter(TimeSpan.FromSeconds(8));

                        var items = await _rssService.GetNewsAsync(
                            x.Feed.Url,
                            x.Feed.Category,
                            cts.Token);

                        var validItems = items
                            .Where(i => i.PublishDate >= cutOffDate)
                            .ToList();

                        _logger?.Info(
                            $"{x.Source.Name} -> {validItems.Count} noticias válidas");

                        Interlocked.Increment(ref successfulFeeds);

                        foreach (var item in validItems)
                        {
                            item.Category = x.Feed.Category;
                            item.Source = x.Source.Name;

                            allItems.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn(
                            $"Error en feed de categoría {x.Feed.Url}: {ex.Message}");
                    }
                });

            // 4. Los feeds existían y estaban activos,
            // pero ninguno pudo devolver noticias correctamente
            if (successfulFeeds == 0)
            {
                throw new HttpRequestException(
                    "No se pudo cargar ningún feed RSS");
            }

            var result = allItems
                .OrderByDescending(x => x.PublishDate)
                .Take(limit)
                .ToList();

            // 5. Los feeds funcionan, pero no hay noticias para esta categoría
            if (result.Count == 0)
            {
                _logger?.Info(
                    $"Categoría '{category}' sin noticias disponibles.");

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.NoContent,
                    Items = result
                };
            }

            _logger?.Info(
                $"Categoría '{category}' " +
                $"- Feeds OK: {successfulFeeds}/{filteredFeeds.Count} | " +
                $"- Noticias: {result.Count}");

            return new NewsQueryResult
            {
                Status = NewsQueryStatus.Success,
                Items = result
            };
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
        public async Task<NewsQueryResult> GetBySourceAsync(string sourceId, int limit = 20)
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

            if (sourceFeeds.Count == 0)
            {
                _logger?.Info(
                    $"Fuente '{sourceId}' sin feeds correspondientes a las categorías activas.");

                return new NewsQueryResult
            {
                Status = NewsQueryStatus.NoContent,
                Items = new List<NewsItem>()
            };
            }
            
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

                return new NewsQueryResult
                {
                    Status = NewsQueryStatus.Success,
                    Items = result
                };
        }
    }


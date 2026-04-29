
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Utilities;
using System.Collections.Concurrent;
using FeedHub_Core.Helpers;

namespace FeedHub_Core.Services;


    public class NewsAggregatorService : INewsAggregatorService
    {
        private readonly IRssService _rssService;
        private readonly FilterPreferencesService _filterService;
        private readonly ILogger _logger;
        public List<string> GetAvailableCategories() => 
                            _feeds.Values.Distinct().OrderBy(x => x).ToList();

        public List<string> GetAvailableSources() => 
                            _feeds.Keys.Select(url => SourceNameSolver.Resolve(url))
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();
                            

        private readonly Dictionary<string, string> _feeds = new()
    {
        //El Pais 

        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/sociedad/portada", "sociedad" },
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/internacional/portada", "internacional" },
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/economia/portada" ,"economia"},
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/ciencia/portada", "ciencia" },
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/tecnologia/portada", "tecnologia"},
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/cultura/portada", "cultura" },
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/deportes/portada", "deportes" },
        {"https://feeds.elpais.com/mrss-s/list/ep/site/elpais.com/section/clima-y-medio-ambiente", "ciencia" },
        {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/espana/portada", "politica" },


        //El Mundo
        {"https://e00-elmundo.uecdn.es/elmundo/rss/internacional.xml", "internacional"},
        {"https://e00-elmundo.uecdn.es/elmundo/rss/economia.xml", "economia"},
        {"https://e00-elmundo.uecdn.es/elmundo/rss/cultura.xml", "cultura" },
        {"https://e00-elmundo.uecdn.es/elmundodeporte/rss/portada.xml", "deportes" },
        {"https://e00-elmundo.uecdn.es/elmundo/rss/espana.xml", "politica" },


        //La Vanguardia
        {"https://www.lavanguardia.com/rss/internacional.xml", "internacional"},
        {"https://www.lavanguardia.com/rss/politica.xml", "politica" },
        {"https://www.lavanguardia.com/rss/deportes.xml", "deportes" },
        {"https://www.lavanguardia.com/rss/economia.xml", "economia" },
        {"https://www.lavanguardia.com/rss/cultura.xml", "cultura" },
        {"https://www.lavanguardia.com/rss/natural.xml", "ciencia" },


        //El Periodico
        {"https://www.elperiodico.com/es/rss/internacional/rss.xml", "internacional" },
        {"https://www.elperiodico.com/es/rss/politica/rss.xml", "politica" },
        {"https://www.elperiodico.com/es/rss/economia/rss.xml", "economia" },
        {"https://www.elperiodico.com/es/rss/tecnologia/rss.xml", "tecnologia" },
        {"https://www.elperiodico.com/es/rss/sociedad/rss.xml", "sociedad" },
        {"https://www.elperiodico.com/es/rss/ciencia/rss.xml", "ciencia" },
        {"https://www.elperiodico.com/es/rss/deportes/rss.xml", "deportes" },
        {"https://www.elperiodico.com/es/rss/ocio-y-cultura/rss.xml", "cultura" },

        //20 Minutos
        {"https://www.20minutos.es/rss/internacional/", "internacional" },
        {"https://www.20minutos.es/rss/deportes/", "deportes" },
        {"https://www.20minutos.es/rss/economia", "economia" },
        {"https://www.20minutos.es/rss/tecnologia/", "tecnologia" },
        {"https://www.20minutos.es/rss/salud/", "ciencia" },
        {"https://www.20minutos.es/rss/videojuegos/", "entretenimiento" },
        {"https://www.20minutos.es/rss/cultura/", "cultura" },

        //El Confidencial
        {"https://rss.elconfidencial.com/espana/", "sociedad" },
        {"https://rss.elconfidencial.com/mundo/", "internacional" },
        {"https://rss.elconfidencial.com/economia/", "economia" },
        {"https://rss.elconfidencial.com/deportes/", "deportes" },
        {"https://rss.elconfidencial.com/cultura/", "cultura" },
        {"https://rss.elconfidencial.com/tecnologia/", "tecnologia" },

        //eldiario.es
        {"https://www.eldiario.es/rss/politica/", "sociedad" },
        {"https://www.eldiario.es/rss/economia/", "economia" },
        {"https://www.eldiario.es/rss/cultura/", "cultura" },
        {"https://www.eldiario.es/rss/internacional/", "internacional"},
        {"https://www.eldiario.es/rss/focos/crisis-climatica/", "ciencia" },
        {"https://www.eldiario.es/rss/tecnologia/", "tecnologia" },

        //BBC Mundo
        {"https://feeds.bbci.co.uk/mundo/temas/ciencia/rss.xml", "internacional" },

        //Xataka
        {"https://www.xataka.com/index.xml", "tecnologia" },

        //Applesfera
        {"https://www.applesfera.com/index.xml", "tecnologia" },

        //Microsiervos
        {"https://www.microsiervos.com/index.xml", "tecnologia" },

        //HyperTextual
        {"https://hipertextual.com/feed","tecnologia" },


        // VidaExtra 
        {"https://www.vidaextra.com/feedburner.xml", "entretenimiento" },
        {"https://www.espinof.com/index.xml", "entretenimiento" },

        //Eurogamer
        {"https://www.eurogamer.es/feed/news", "entretenimiento" },

        // 3DJuegos 
        {"https://www.3djuegos.com/index.xml", "entretenimiento"},

        // HobbyConsolas 
        {"https://www.hobbyconsolas.com/rss", "entretenimiento"},

        //IGN España
        {"https://es.ign.com/playstation-5.xml", "entretenimiento" },
        {"https://es.ign.com/nintendo.xml", "entretenimiento" },
        {"https://es.ign.com/xbox.xml", "entretenimiento" },
        {"https://es.ign.com/pc.xml", "entretenimiento" },

        
        //ElTiempo.es
        {"https://www.eltiempo.es/noticias/feed", "ciencia" },

        //EFE Verde
        {"https://efeverde.com/feed/", "ciencia" },

        //Econoticias
        {"https://www.ecoticias.com/feed/", "ciencia" },

        //Astronomia
        {"https://www.esa.int/rssfeed/Spain", "ciencia"},
        {"https://astroaficion.com/feed/", "ciencia" },
        {"https://fronteraespacial.com/feed/", "ciencia" },
    };

        public NewsAggregatorService(IRssService rssService, ILogger logger, FilterPreferencesService filterService)
        {
            _rssService = rssService;
            _logger = logger;
            _filterService = filterService;
        }

        public async Task<List<NewsItem>> GetLatestMixedAsync(int limit)
        {
            var tempList = new ConcurrentBag<NewsItem>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };

            System.Diagnostics.Debug.WriteLine("#debug aggregator [LATEST] Aplicando filtros...");

            var filteredFeeds = _feeds.Where(kvp =>
            {
                var sourceName = SourceNameSolver.Resolve(kvp.Key);
                var srcOk = _filterService.IsSourceActive(sourceName);
                var catOk = _filterService.IsCategoryActive(kvp.Value);

                if (!srcOk || !catOk)
                 System.Diagnostics.Debug.WriteLine($"#debug aggregator [LATEST] BLOQUEADO: {sourceName} | cat:{kvp.Value} | srcOk:{srcOk} catOk:{catOk}");
                return _filterService.IsCategoryActive(kvp.Value) && _filterService.IsSourceActive(sourceName);

            }).ToList();

            System.Diagnostics.Debug.WriteLine($"#debug aggregator [LATEST] Feeds tras filtro: {filteredFeeds.Count}/{_feeds.Count}");


            await Parallel.ForEachAsync(filteredFeeds, options, async (kvp, ct) =>
            {
                try
                {
                    // Timeout individual de 10 seg por feed
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(10));

                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);

                    System.Diagnostics.Debug.WriteLine($"#debug feeds [{SourceNameSolver.Resolve(kvp.Key)}] {items.Count} artículos obtenidos");

                    var latestTwo = items.OrderByDescending(i => i.PublishDate).Take(2);

                    foreach (var item in latestTwo)
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

            var result = tempList.OrderByDescending(x => x.PublishDate)
                       .Take(limit)
                       .ToList();

            System.Diagnostics.Debug.WriteLine($"#debug ui [LATEST] Artículos enviados a UI: {result.Count}");
            return result;
        }
    

        public async Task<List<NewsItem>> GetByCategoryAsync(string category, int limit)
        {
            var allItems = new ConcurrentBag<NewsItem>();
            DateTime cutOffDate = category == "ciencia" || category == "cultura" || category == "entretenimiento"
                                ? DateTime.Now.AddDays(-2)
                                : DateTime.Now.AddDays(-7);

            System.Diagnostics.Debug.WriteLine($"#debug aggregator [CATEGORY] Cargando '{category}' con filtros...");

            var filteredFeeds = _feeds.Where(kvp =>
            {
                    if (!kvp.Value.Equals(category, StringComparison.OrdinalIgnoreCase)) return false;
                    var sourceName = SourceNameSolver.Resolve(kvp.Key);
                    var srcOk = _filterService.IsSourceActive(sourceName);
                    if (!srcOk)
                        System.Diagnostics.Debug.WriteLine($"#debug aggregator [CATEGORY] BLOQUEADO: {sourceName}");
                    return srcOk;
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"#debug aggregator [CATEGORY] Feeds tras filtro: {filteredFeeds.Count}");

            await Parallel.ForEachAsync(filteredFeeds, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (kvp, ct) => //number of HTTP conections
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);

                    System.Diagnostics.Debug.WriteLine($"#debug feeds [{SourceNameSolver.Resolve(kvp.Key)}] {items.Count} artículos obtenidos");

                    foreach (var item in items)
                    {
                        if (item.PublishDate >= cutOffDate)
                        {
                            item.Category = kvp.Value;
                            item.Source = SourceNameSolver.Resolve(item.Link);
                            allItems.Add(item);
                        }

                    }
                }
                catch { /* Ignorar errores de red */}
            });

            var result = allItems.OrderByDescending(x => x.PublishDate).Take(limit).ToList();
            System.Diagnostics.Debug.WriteLine($"#debug ui [CATEGORY] Artículos enviados a UI: {result.Count}");
            return result;
        }

        public async Task<IEnumerable<NewsItem>> SearchByKeywordAsync(string query, int limit = 40)
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<NewsItem>();

            var allResults = new ConcurrentBag<NewsItem>();

            // Buscamos en todos los feeds en paralelo para encontrar la palabra clave
            await Parallel.ForEachAsync(_feeds, new ParallelOptions { MaxDegreeOfParallelism = 20 }, async (kvp, ct) =>
            {
                try
                {
                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, ct);
                    var filtered = items.Where(n =>
                        (n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

                    foreach (var item in filtered)
                    {
                        item.Source = SourceNameSolver.Resolve(item.Link);
                        allResults.Add(item);
                    }
                }
                catch { /* fail silently */ }
            });

            return allResults.OrderByDescending(n => n.PublishDate).Take(limit);
        }
        public async Task<List<NewsItem>> GetBySourceAsync(string sourceId, int limit = 20)
        {
            var allItems = new ConcurrentBag<NewsItem>();
            var cleanSourceId = sourceId.ToLower().Replace(".", "");

            System.Diagnostics.Debug.WriteLine($"#debug aggregator [SOURCE] Cargando fuente '{sourceId}' con filtros...");

            var sourceFeeds = _feeds.Where(kvp =>
            {
                if (!kvp.Key.ToLower().Replace(".", "").Contains(cleanSourceId)) return false;
                var catOk = _filterService.IsCategoryActive(kvp.Value);
                if (!catOk)
                    System.Diagnostics.Debug.WriteLine($"#debug aggregator [SOURCE] BLOQUEADO: cat '{kvp.Value}' desactivada");
                return catOk;
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"#debug aggregator [SOURCE] Feeds tras filtro: {sourceFeeds.Count}");

            await Parallel.ForEachAsync(sourceFeeds, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (kvp, ct) =>
            {
                try
                {
                    var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, ct);
                    System.Diagnostics.Debug.WriteLine($"#debug feeds [{kvp.Value}] {items.Count} artículos obtenidos");
                    foreach (var item in items)
                    {
                        item.Source = SourceNameSolver.Resolve(item.Link);
                        allItems.Add(item);
                    }
                }
                catch { /* Silencio */ }
            });

            var result = allItems.OrderByDescending(x => x.PublishDate).Take(limit).ToList();
            System.Diagnostics.Debug.WriteLine($"#debug ui [SOURCE] Artículos enviados a UI: {result.Count}");
            return result;
        }
    }


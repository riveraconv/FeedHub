
using FeedHub_Core.Interfaces;
using FeedHub_Core.Models;
using FeedHub_Core.Services;
using FeedHub_Core.Utilities;
using System.Collections.Concurrent;

public class NewsAggregatorService : INewsAggregatorService
{
    private readonly IRssService _rssService;
    private readonly ILogger _logger;

    private readonly Dictionary<string, string> _feeds = new()
    {
        /*
        //El Pais 

            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/sociedad/portada", "society"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/internacional/portada", "international" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/economia/portada" ,"economy"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/ciencia/portada", "science" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/tecnologia/portada", "technology"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/cultura/portada", "culture" },
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/deportes/portada", "sports" },
            {"https://feeds.elpais.com/mrss-s/list/ep/site/elpais.com/section/clima-y-medio-ambiente", "climatology"},
            {"https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/section/espana/portada", "politics" },
           
            
            //El Mundo
            {"https://e00-elmundo.uecdn.es/elmundo/rss/internacional.xml", "international"},
            {"https://e00-elmundo.uecdn.es/elmundo/rss/economia.xml", "economy"},
            {"https://e00-elmundo.uecdn.es/elmundo/rss/cultura.xml", "culture" },
            {"https://e00-elmundo.uecdn.es/elmundodeporte/rss/portada.xml", "sports" },
            {"https://e00-elmundo.uecdn.es/elmundo/rss/espana.xml", "politics" },

           
            //La Vanguardia
            {"https://www.lavanguardia.com/rss/internacional.xml", "international"},
            {"https://www.lavanguardia.com/rss/politica.xml", "politics" },
            {"https://www.lavanguardia.com/rss/deportes.xml", "sports" },
            {"https://www.lavanguardia.com/rss/economia.xml", "economy" },
            {"https://www.lavanguardia.com/rss/cultura.xml", "culture" },
            {"https://www.lavanguardia.com/rss/natural.xml", "science" },
       
            
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
            {"https://www.20minutos.es/rss/salud/", "science" },
            {"https://www.20minutos.es/rss/videojuegos/", "videogames" },
            {"https://www.20minutos.es/rss/cultura/", "culture" },
        
            //El Confidencial
            {"https://rss.elconfidencial.com/espana/", "Spain" },
            {"https://rss.elconfidencial.com/mundo/", "international" },
            {"https://rss.elconfidencial.com/economia/", "economy" },
            {"https://rss.elconfidencial.com/deportes/", "sports" },
            {"https://rss.elconfidencial.com/cultura/", "culture" },
            {"https://rss.elconfidencial.com/tecnologia/", "technology" },

            //eldiario.es
            {"https://www.eldiario.es/rss/politica/", "politics" },
            {"https://www.eldiario.es/rss/economia/", "economy" },
            {"https://www.eldiario.es/rss/cultura/", "culture" },
            {"https://www.eldiario.es/rss/internacional/", "international"},
            {"https://www.eldiario.es/rss/focos/crisis-climatica/", "climatology" },
            {"https://www.eldiario.es/rss/tecnologia/", "technology" },

            //Xataka
            {"https://feeds.feedburner.com/xataka2", "technology" },

            //Applesfera
            {"https://www.applesfera.com/index.xml", "technology" },
        */
            //IGN España
            {"https://es.ign.com/news.xml", "videogames" },

            // VidaExtra 
            {"https://www.vidaextra.com/index.xml", "videogames"},

            //Eltiempo.es
            {"https://www.eurogamer.es/feed/news", "climatology" },

            // 3DJuegos 
            {"https://www.3djuegos.com/index.xml", "videogames"},

            // Eurogamer España 
            {"https://www.eurogamer.es/feed", "videogames"},

            // ComputerHoy 
            {"https://feeds.feedburner.com/computerhoy", "technology"},   

            // HobbyConsolas 
            {"https://feeds.feedburner.com/hobbyconsolas", "videogames"},
        
    };



    public NewsAggregatorService(IRssService rssService, ILogger logger)
    {
        _rssService = rssService;
        _logger = logger;
    }

    public async Task<List<NewsItem>> GetLatestMixedAsync(int limit)
    {
        var tempList = new ConcurrentBag<NewsItem>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 5
        };

        await Parallel.ForEachAsync(_feeds, options, async (kvp, ct) =>
        {
            try
            {
                // TIMEOUT: Si un feed tarda > 5 segundos, se ignora
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);
                var latest = items.OrderByDescending(i => i.PublishDate).FirstOrDefault();

                if (latest != null)
                {
                    latest.Category = kvp.Value;
                    tempList.Add(latest);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error en {kvp.Key}: {ex.Message}");
            }
        });

        var random = new Random();
        return tempList
            .OrderBy(x => random.Next()) // Mezclamos para dar variedad
            .Take(limit)
            .ToList();
    }

    public async Task<List<NewsItem>> GetByCategoryAsync(string category, int limit)
    {
        var allItems = new ConcurrentBag<NewsItem>();
        DateTime cutOffDate = category == "science" || category == "culture" || category == "videogames"
                            ? DateTime.Now.AddDays(-2)
                            : DateTime.Now.AddDays(-7); 

        var filteredFeeds = _feeds.Where(kvp => kvp.Value.Equals(category, StringComparison.OrdinalIgnoreCase));

        await Parallel.ForEachAsync(filteredFeeds, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (kvp, ct) => //number of HTTP conections
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var items = await _rssService.GetNewsAsync(kvp.Key, kvp.Value, cts.Token);

                var recentItems = items.Where(x => x.PublishDate >= cutOffDate);

                foreach (var item in recentItems)
                {
                    item.Category = kvp.Value;
                    allItems.Add(item);
                }
            }
            catch { /* Ignorar errores de red */ }
        });

        var finalResult = allItems.OrderByDescending(x => x.PublishDate).Take(limit).ToList();

        // If the filter cannot pull any result, will pull any date available to not made an empty list.
        if (!finalResult.Any())
        {
            return allItems.OrderByDescending(x => x.PublishDate).Take(5).ToList();
        }

        return finalResult;
    }
}

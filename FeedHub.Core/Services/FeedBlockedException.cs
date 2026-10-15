namespace FeedHub_Core.Services;

/// <summary>
/// Señal interna de RssService hacia NewsAggregatorService: el servidor ha respondido
/// con un reto anti-bots (por ejemplo 202 sin contenido) en lugar de servir el feed.
/// No debe llegar a los ViewModels: el agregador la convierte en NewsQueryStatus.Blocked.
/// </summary>
public class FeedBlockedException : Exception
{
    public string? FeedUrl { get; }

    public FeedBlockedException(string message, string? feedUrl = null)
        : base(message)
    {
        FeedUrl = feedUrl;
    }
}
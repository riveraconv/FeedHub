using System.Reflection;
using System.Text.Json;
using FeedHub_Core.Models;

namespace FeedHub_Core.Services;

public class SourceCatalogService
{
    private const string ResourceName =
        "FeedHub_Core.Configuration.news-sources.json";

    private readonly Lazy<IReadOnlyList<FeedSourceConfig>> _sources;

    public SourceCatalogService()
    {
        _sources = new Lazy<IReadOnlyList<FeedSourceConfig>>(LoadSources);
    }

    public IReadOnlyList<FeedSourceConfig> GetSources()
    {
        return _sources.Value;
    }

    private static IReadOnlyList<FeedSourceConfig> LoadSources()
    {
        var assembly = typeof(SourceCatalogService).Assembly;

        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso integrado '{ResourceName}'.");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var sources = JsonSerializer.Deserialize<List<FeedSourceConfig>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidOperationException(
                "El catálogo de fuentes está vacío o no tiene un JSON válido.");

        return sources;
    }
    public FeedSourceConfig? GetSourceById(string id)
    {
        return GetSources().FirstOrDefault(s =>
            s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public string GetSourceName(string id) =>
        GetSourceById(id)?.Name ?? "Fuente desconocida";
}
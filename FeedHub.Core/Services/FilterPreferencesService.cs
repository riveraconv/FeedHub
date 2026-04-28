using System.Text.Json;
using FeedHub_Core.Interfaces;


namespace FeedHub_Core.Services;

public class FilterPreferencesService
{
    private readonly IPreferencesService _prefs;
    private const string ActiveSourcesKey = "active_sources";
    private const string ActiveCategoriesKey = "active_categories";
    public FilterPreferencesService(IPreferencesService prefs)
    {
        _prefs = prefs;
    }

    // Obtenemos el conjunto de fuentes activas (si no hay nada, devolvemos un HashSet vacío)
    public HashSet<string> GetActiveSources()
    {
        var json = _prefs.Get(ActiveSourcesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return new HashSet<string>();
        
        return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
    }

    // Obtenemos el conjunto de categorías activas
    public HashSet<string> GetActiveCategories()
    {
        var json = _prefs.Get(ActiveCategoriesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return new HashSet<string>();
        
        return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
    }

    public void SetSourceActive(string sourceId, bool active)
    {
        var sources = GetActiveSources();
        if (active) sources.Add(sourceId);
        else sources.Remove(sourceId);

        var json = JsonSerializer.Serialize(sources);
        _prefs.Set(ActiveSourcesKey, json);
    }

    public void SetCategoryActive(string category, bool active)
    {
        var categories = GetActiveCategories();
        if (active) categories.Add(category);
        else categories.Remove(category);

        var json = JsonSerializer.Serialize(categories);
        _prefs.Set(ActiveCategoriesKey, json);
    }

    // Estos son los métodos que usa el NewsAggregatorService para filtrar
    public bool IsSourceActive(string sourceId)
    {
        var sources = GetActiveSources();
        // Si la lista está vacía, por defecto asumimos que TODO está activo (primera carga)
        if (sources.Count == 0) return true; 
        return sources.Contains(sourceId);
    }

    public bool IsCategoryActive(string category)
    {
        var categories = GetActiveCategories();
        // Si no hay filtros guardados, mostramos todo
        if (categories.Count == 0) return true;
        return categories.Contains(category);
    }
}

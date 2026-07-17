using System.Text.Json;
using FeedHub_Core.Interfaces;

namespace FeedHub_Core.Services;

public class FilterPreferencesService
{
    private readonly IPreferencesService _prefs;
    private const string DisabledSourcesKey = "disabled_sources";
    private const string DisabledCategoriesKey = "disabled_categories";

    public FilterPreferencesService(IPreferencesService prefs)
    {
        _prefs = prefs;
    }

    private HashSet<string> GetDisabledSources()
    {
        var json = _prefs.Get(DisabledSourcesKey, string.Empty);

        return string.IsNullOrWhiteSpace(json)
        ? new() 
        : JsonSerializer.Deserialize<HashSet<string>>(json) ?? new();

    }

    private HashSet<string> GetDisabledCategories()
    {
        var json = _prefs.Get(DisabledCategoriesKey, string.Empty);
        
        return string.IsNullOrWhiteSpace(json)
        ? new()
        : JsonSerializer.Deserialize<HashSet<string>>(json) ?? new();
    }

    public void SetSourceActive(string sourceId, bool active)
    {
        var disabled = GetDisabledSources();
        if (!active) disabled.Add(sourceId);
        else disabled.Remove(sourceId);

        var json =  JsonSerializer.Serialize(disabled);
        _prefs.Set(DisabledSourcesKey, json);
    }

    public void SetCategoryActive(string category, bool active)
    {
        var disabled = GetDisabledCategories();
        if (!active) disabled.Add(category);
        else disabled.Remove(category);

        var json = JsonSerializer.Serialize(disabled);
        _prefs.Set(DisabledCategoriesKey, json);
    }

    public bool IsSourceActive(string sourceId)
    {
        return !GetDisabledSources().Contains(sourceId);
    }

    public bool IsCategoryActive(string category)
    {   
        return !GetDisabledCategories().Contains(category);
    }
}

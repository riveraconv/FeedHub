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
        if (string.IsNullOrWhiteSpace(json)) return new HashSet<string>();
        return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
    }

    private HashSet<string> GetDisabledCategories()
    {
        var json = _prefs.Get(DisabledCategoriesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return new HashSet<string>();
        return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
    }

    public void SetSourceActive(string sourceId, bool active)
    {
        var disabled = GetDisabledSources();
        if (!active) disabled.Add(sourceId);
        else disabled.Remove(sourceId);
        _prefs.Set(DisabledSourcesKey, JsonSerializer.Serialize(disabled));
    }

    public void SetCategoryActive(string category, bool active)
    {
        var disabled = GetDisabledCategories();
        if (!active) disabled.Add(category);
        else disabled.Remove(category);
        _prefs.Set(DisabledCategoriesKey, JsonSerializer.Serialize(disabled));
    }

    public bool IsSourceActive(string sourceId)
    {
        var disabled = GetDisabledSources();
        return !disabled.Contains(sourceId);
    }

    public bool IsCategoryActive(string category)
    {
        var disabled = GetDisabledCategories();
        return !disabled.Contains(category);
    }
}

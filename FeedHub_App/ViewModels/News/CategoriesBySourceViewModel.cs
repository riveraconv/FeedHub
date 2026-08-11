using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FeedHub_App.Views.News;
using FeedHub_Core.Services;

namespace FeedHub_App.ViewModels.News;

// 1. DEFINICIÓN DE LOS MODELOS (Fuera para que todos los vean)
public class NewsSource
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class SourceGroup : ObservableCollection<NewsSource>
{
    public string CategoryTitle { get; private set; }
    public SourceGroup(string title, IEnumerable<NewsSource> sources) : base(sources)
    {
        CategoryTitle = title;
    }
}

// 2. VIEWMODEL
public partial class CategoriesBySourceViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SourceGroup> groupedSources;
    [ObservableProperty]
    private NewsSource selectedSource;
    public CategoriesBySourceViewModel(SourceCatalogService sourceCatalog)
    {
        var allSources = sourceCatalog
            .GetSources()
            .Select(source => new NewsSource
            {
                Id = source.Id,
                Name = source.Name.ToUpperInvariant(),
                Description = source.Description,
                Domain = source.Domain,
                Category = source.Group
            })
            .ToList();
        
        // Agrupamos y convertimos cada grupo explícitamente a nuestra clase SourceGroup
        var groups = allSources
            .GroupBy(source => source.Category)
            .Select(group => new SourceGroup
            (group.Key,
             group.ToList()));

        GroupedSources = new ObservableCollection<SourceGroup>(groups);
    }


    [RelayCommand]
    private async Task SelectSource(string sourceId)
    {
        if (!string.IsNullOrEmpty(sourceId))
            await Shell.Current.GoToAsync($"{nameof(NewsBySourcePage)}?source={sourceId}");
    }
}

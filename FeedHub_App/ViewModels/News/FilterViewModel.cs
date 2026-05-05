using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FeedHub_Core.Services;
using FeedHub_Core.Models;
using System.Text.RegularExpressions;


namespace FeedHub_App.ViewModels;

public partial class FilterViewModel : ObservableObject
{
    // En FilterViewModel.cs
    [ObservableProperty]
    private bool showCategories = true;

    private readonly FilterPreferencesService _filterService;
    private readonly INewsAggregatorService _aggregator;

    public ObservableCollection<FilterItem> Categories { get; } = new();
    public ObservableCollection<FilterItem> Sources { get; } = new();

    public FilterViewModel(FilterPreferencesService filterService, INewsAggregatorService aggregator)
    {
        _filterService = filterService;
        _aggregator = aggregator;
    }

    private async void LoadFilters()
    {
        System.Diagnostics.Debug.WriteLine("#debug filters [LOAD] Cargando filtros...");

        Categories.Clear();
        foreach (var cat in _aggregator.GetAvailableCategories())
        {
            var isActive = _filterService.IsCategoryActive(cat);
            System.Diagnostics.Debug.WriteLine($"#debug filters [LOAD CAT] '{cat}' → {(isActive ? "activa" : "desactivada")}");

            Categories.Add(new FilterItem
            {
                Title = cat.ToUpper(),
                Code = cat,
                IsActive = isActive
            });
        }

        Sources.Clear();
        foreach (var src in _aggregator.GetAvailableSources())
        {                    
            var isActive = _filterService.IsSourceActive(src);
            System.Diagnostics.Debug.WriteLine($"#debug filters [LOAD SRC] '{src}' → {(isActive ? "activa" : "desactivada")}");

            Sources.Add(new FilterItem
            {
                Title = src,
                Code = src,
                IsActive = isActive
            });
        }

        System.Diagnostics.Debug.WriteLine($"#debug filters [LOAD] Categorías: {Categories.Count} | Fuentes: {Sources.Count}");
    }

    	public void LoadFiltersIfNeeded()
        {
            if (Categories.Count > 0 || Sources.Count > 0) return;
            LoadFilters();
        }

    // Comando para guardar cuando el usuario toca el Switch
    public void SavePreference(FilterItem item, bool isCategory)
    {
        System.Diagnostics.Debug.WriteLine($"#debug filters [SAVE] {(isCategory ? "Categoría" : "Fuente")} '{item.Code}' → {(item.IsActive ? "activa" : "desactivada")}");

        if (isCategory)
            _filterService.SetCategoryActive(item.Code, item.IsActive);
        else
            _filterService.SetSourceActive(item.Code, item.IsActive);
    }
    [RelayCommand]
    private void ChangeTab(string tab)
    {
        ShowCategories = (tab == "cat");
        System.Diagnostics.Debug.WriteLine($"#debug ui [TAB] Cambiando a {(ShowCategories ? "CATEGORÍAS" : "FUENTES")}");
    }
}

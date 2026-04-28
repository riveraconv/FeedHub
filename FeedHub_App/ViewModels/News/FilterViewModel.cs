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

        LoadFilters();
    }

    private void LoadFilters()
    {
        Categories.Clear();
        foreach (var cat in _aggregator.GetAvailableCategories())
        {
            Categories.Add(new FilterItem
            {
                Title = cat.ToUpper(),
                Code = cat,
                IsActive = _filterService.IsCategoryActive(cat)
            });
        }

        Sources.Clear();
        foreach (var src in _aggregator.GetAvailableSources())
        {
            string cleanSrc = src.ToLower().Trim();
            string friendlyName = src;

            if (cleanSrc.Contains("elmundo") || cleanSrc.Contains("e00"))
            {
                friendlyName = "El Mundo";
            }
            else if (cleanSrc == "feeds" || cleanSrc.Contains("bbc"))
            {
                friendlyName = "BBC News";
            }
                        
            Sources.Add(new FilterItem
            {
                Title = friendlyName,
                Code = src,
                IsActive = _filterService.IsSourceActive(src),
            });
        }
    }

    // Comando para guardar cuando el usuario toca el Switch
    public void SavePreference(FilterItem item, bool isCategory)
    {
        if (isCategory)
            _filterService.SetCategoryActive(item.Code, item.IsActive);
        else
            _filterService.SetSourceActive(item.Code, item.IsActive);
    }
    [RelayCommand]
    private void ChangeTab(string tab)
    {
        ShowCategories = (tab == "cat");
    }
}

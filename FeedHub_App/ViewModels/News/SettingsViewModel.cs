using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeedHub_App.Views.Settings;
using FeedHub_Core.Services;

namespace FeedHub_App.ViewModels.Settings;

public partial class SettingsViewModel : ObservableObject
{
    private readonly QuickArticleCacheService _cacheService;
    [ObservableProperty]
    private bool _isDarkMode;
    [ObservableProperty]
    string _appVersion;

    public SettingsViewModel(QuickArticleCacheService cacheService)
    {
        _cacheService = cacheService;

        // Al arrancar, leemos de Preferences. 
        // Si no existe, usamos el tema actual del sistema.
        IsDarkMode = Preferences.Default.Get("DarkMode", Application.Current.RequestedTheme == AppTheme.Dark);
        _appVersion = $"V.{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";
    }

    // Este m�todo se ejecuta AUTOM�TICAMENTE cuando cambias el Switch
    // porque el Toolkit detecta que la propiedad "IsDarkMode" ha cambiado.
    partial void OnIsDarkModeChanged(bool value)
    {
        Preferences.Default.Set("DarkMode", value);

        // Aplicamos el tema visual a toda la aplicaci�n
        Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    [RelayCommand]
    private async Task ClearCache()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Limpiar caché",
            "Se eliminará la caché temporal de noticias.",
            "Sí",
            "No");

        if (!confirm)
            return;

        _cacheService.Clear();

        await Shell.Current.DisplayAlert(
            "Listo",
            "La caché de noticias se ha borrado.",
            "OK");
    }

    [RelayCommand]
    public async Task GoBackToMainMenu()
    {
        await Shell.Current.GoToAsync("..");
    }
    [RelayCommand]
    public async Task GoToFilters()
    {
        await Shell.Current.GoToAsync(nameof(SelectFilterPage));
    }

    
}
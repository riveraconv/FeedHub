using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FeedHub_App.ViewModels.Settings;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkMode;

    public SettingsViewModel()
    {
        // Al arrancar, leemos de Preferences. 
        // Si no existe, usamos el tema actual del sistema.
        IsDarkMode = Preferences.Default.Get("DarkMode", Application.Current.RequestedTheme == AppTheme.Dark);
    }

    // Este método se ejecuta AUTOMÁTICAMENTE cuando cambias el Switch
    // porque el Toolkit detecta que la propiedad "IsDarkMode" ha cambiado.
    partial void OnIsDarkModeChanged(bool value)
    {
        // Guardamos el valor permanentemente
        Preferences.Default.Set("DarkMode", value);

        // Aplicamos el tema visual a toda la aplicación
        Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    [RelayCommand]
    private async Task ClearCache()
    {
        bool confirm = await Shell.Current.DisplayAlert("Clear Cache", "Are you sure you want to delete temporary data?", "Yes", "No");
        if (confirm)
        {
            await Task.Delay(1000); // Simulación
            await Shell.Current.DisplayAlert("Done", "News Cache was cleared", "OK");
        }
    }

    [RelayCommand]
    public async Task GoBackToMainMenu()
    {
        await Shell.Current.GoToAsync("..");
    }
}
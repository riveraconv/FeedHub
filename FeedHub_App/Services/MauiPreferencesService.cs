using FeedHub_Core.Interfaces;
using Microsoft.Maui.Storage;

namespace FeedHub_App.Services;

public class MauiPreferencesService : IPreferencesService
{
    // Usamos el Preferences nativo de MAUI
    public string Get(string key, string defaultValue) => 
        Preferences.Default.Get(key, defaultValue);

    public void Set(string key, string value) => 
        Preferences.Default.Set(key, value);
}

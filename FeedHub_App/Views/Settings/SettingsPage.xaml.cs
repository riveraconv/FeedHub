using FeedHub_App.ViewModels.Settings;

namespace FeedHub_App.Views.Settings;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
protected override bool OnBackButtonPressed()
{
    // Como ahora todas son "Modales" para el sistema, 
    // usamos Navigation.PopModalAsync() para cerrar.
    Dispatcher.Dispatch(async () => 
    {
        await Shell.Current.Navigation.PopModalAsync();
    });

    return true; // Bloquea el cierre de la App
}
}

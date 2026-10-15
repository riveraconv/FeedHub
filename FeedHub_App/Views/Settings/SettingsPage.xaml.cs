
using FeedHub_App.ViewModels.Settings;

#if ANDROID
using Microsoft.Extensions.DependencyInjection;
using Plugin.AdMob.Services;
#endif

namespace FeedHub_App.Views.Settings;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        var consentService =
            IPlatformApplication.Current.Services
                .GetService<IAdConsentService>();

        PrivacyOptionsCard.IsVisible =
            consentService?.IsPrivacyOptionsRequired() == true;
#else
        PrivacyOptionsCard.IsVisible = false;
#endif
    }

    private async void OnPrivacyOptionsClicked(
        object sender, EventArgs e)
    {
#if ANDROID
        try
        {
            var consentService =
                IPlatformApplication.Current.Services
                    .GetService<IAdConsentService>();

            if (consentService?.IsPrivacyOptionsRequired() == true)
            {
                consentService.ShowPrivacyOptionsForm();
            }
            else
            {
                PrivacyOptionsCard.IsVisible = false;
            }
        }
        catch (Exception)
        {
            await DisplayAlert(
                "Privacidad",
                "No se han podido abrir las opciones de privacidad. Inténtalo de nuevo.",
                "Aceptar");
        }
#endif
    }
}


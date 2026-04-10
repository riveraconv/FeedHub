using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using Android.Views;

namespace FeedHub_App.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme",
               MainLauncher = true, 
               ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetBackgroundDrawable(new global::Android.Graphics.Drawables.ColorDrawable(global::Android.Graphics.Color.ParseColor("#0D253F")));
            WindowCompat.SetDecorFitsSystemWindows(Window!, false);
            Window!.SetNavigationBarColor(global::Android.Graphics.Color.Transparent);
            Window!.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
            Window!.DecorView.SetBackgroundColor(
                global::Android.Graphics.Color.ParseColor("#0d253f"));

            // Iconos claros en ambas barras (para fondo oscuro)
            var controller = WindowCompat.GetInsetsController(Window!, Window!.DecorView);
            controller.AppearanceLightNavigationBars = false;
            controller.AppearanceLightStatusBars = false;
        }

    }
}

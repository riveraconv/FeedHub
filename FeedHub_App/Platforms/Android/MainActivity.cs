using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace FeedHub_App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // 1. Cambiamos el color de la barra de navegación (la de abajo)
            // Puedes ponerlo transparente o del color de tu fondo
            Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#121212"));

            // 2. Opcional: Cambiar también la barra de estado (la de arriba donde está la batería)
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#121212"));

            // 3. Importante: Decirle al sistema que use iconos claros sobre fondo oscuro
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                // Usamos el flag 'AppearanceLightNavigationBars' para controlar el color de los iconos
                // Al NO incluirlo (0), los iconos se vuelven blancos (ideales para fondo oscuro)
                Window.InsetsController?.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightNavigationBars);
                Window.InsetsController?.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightStatusBars);
            }
        }
    }
}

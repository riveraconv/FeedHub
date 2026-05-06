using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using Android.Views;

namespace FeedHub_App.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme",
           MainLauncher = true,
           WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateAlwaysHidden,
           ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
           ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Window.SetBackgroundDrawable(
                new global::Android.Graphics.Drawables.ColorDrawable(
                    global::Android.Graphics.Color.ParseColor("#0F172A")));

            WindowCompat.SetDecorFitsSystemWindows(Window!, false);
            Window!.SetNavigationBarColor(
                global::Android.Graphics.Color.Transparent);
            Window!.SetStatusBarColor(
                global::Android.Graphics.Color.Transparent);
            Window!.DecorView.SetBackgroundColor(
                global::Android.Graphics.Color.ParseColor("#0F172A"));

            var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme != AppTheme.Light;

            // Iconos claros en ambas barras (para fondo oscuro)
            var controller = WindowCompat.GetInsetsController(Window!, Window!.DecorView);
            controller.AppearanceLightNavigationBars = !isDark;
            controller.AppearanceLightStatusBars = !isDark;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                Window!.NavigationBarContrastEnforced = false;
                Window!.StatusBarContrastEnforced = false;
            }
        
            ViewCompat.SetOnApplyWindowInsetsListener(Window!.DecorView, new KeyboardInsetsListener());
        }

    }
    public class KeyboardInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(global::Android.Views.View view, WindowInsetsCompat insets)
        {
            // 1. Obtenemos los insets del teclado (IME)
            var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            
            // 2. Comprobamos si el teclado está visible (Esta es la línea que faltaba)
            bool isKeyboardVisible = insets.IsVisible(WindowInsetsCompat.Type.Ime());

            // 3. Aplicamos el padding:
            // Si el teclado está abierto, usamos su altura (ime.Bottom).
            // Si está cerrado, usamos 0 para que el contenido fluya detrás de la barra de navegación.
            int paddingBottom = isKeyboardVisible ? ime.Bottom : 0;

            view.SetPadding(0, 0, 0, paddingBottom);

            // Muy importante: devolvemos los insets originales para que el sistema 
            // siga sabiendo qué espacio hay, pero nosotros ya manejamos el padding visual.
            return insets;
        }
    }
}

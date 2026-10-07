using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using Android.Views;
using Android.Util;

namespace FeedHub_App.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme",
           MainLauncher = true,
           WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateAlwaysHidden,
           ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
           ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
                private const string Tag = "FeedHubEdge";

        protected override void OnResume()
        {
            base.OnResume();
            LogTheme("OnResume");
        }

private void LogTheme(string origin)
{
    var window = Window;
    if (window is null || Theme is null)
        return;

    var release = Build.VERSION.Release;
    var sdk = (int)Build.VERSION.SdkInt;
    var target = ApplicationInfo?.TargetSdkVersion.ToString() ?? "?";
    var windowBackground = ResolveColor(global::Android.Resource.Attribute.WindowBackground);
    var navBarTheme = ResolveColor(global::Android.Resource.Attribute.NavigationBarColor);
    var statusBarTheme = ResolveColor(global::Android.Resource.Attribute.StatusBarColor);
    var primaryTheme = ResolveColor(global::Android.Resource.Attribute.ColorAccent);
    var primaryDarkTheme = ResolveColor(global::Android.Resource.Attribute.ColorPrimaryDark);
    var navBarWindow = window.NavigationBarColor.ToString("X8");
    var decorSystemUiVisibility = (int)window.DecorView.SystemUiVisibility;

    Log.Info(Tag,
        $"{origin}: android={release} (sdk {sdk}), target={target}, " +
        $"theme.windowBackground={windowBackground}, " +
        $"theme.navigationBarColor={navBarTheme}, " +
        $"theme.statusBarColor={statusBarTheme}, " +
        $"theme.colorAccent={primaryTheme}, " +
        $"theme.colorPrimaryDark={primaryDarkTheme}, " +
        $"window.navigationBarColor=0x{navBarWindow}, " +
        $"decorSystemUiVisibility=0x{decorSystemUiVisibility:X8}");
}

        private string ResolveColor(int attribute)
        {
            var value = new TypedValue();
            return Theme!.ResolveAttribute(attribute, value, true)
                ? "0x" + value.Data.ToString("X8")
                : "(sin definir)";
        }
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                Window.NavigationBarContrastEnforced = false;

            WindowCompat.SetDecorFitsSystemWindows(Window!, false);

            ViewCompat.SetOnApplyWindowInsetsListener(
                Window!.DecorView,
                new KeyboardInsetsListener());
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

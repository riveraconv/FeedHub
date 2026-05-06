using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using CommunityToolkit.Maui.Core;
using System.Diagnostics;

namespace FeedHub_App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.IO.File.WriteAllText(
                System.IO.Path.Combine(FileSystem.AppDataDirectory, "crash.log"),
                $"{ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                    System.IO.File.WriteAllText(
                    System.IO.Path.Combine(FileSystem.AppDataDirectory, "task_crash.log"),
                    $"{e.Exception.Message}\n{e.Exception.StackTrace}");
                e.SetObserved();
            };
            RequestedThemeChanged += OnThemeChanged;
        }
        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            UpdateSystemBars(e.RequestedTheme);
        }
        public static void UpdateSystemBars(AppTheme theme)
        {
    #if ANDROID
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var activity = Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
                if (activity?.Window == null) return;
                var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(
                    activity.Window, activity.Window.DecorView);
                bool isLight = theme == AppTheme.Light;
                controller.AppearanceLightStatusBars = isLight;
                controller.AppearanceLightNavigationBars = isLight;
            });
    #endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            return window;
        }
    }
}

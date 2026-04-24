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
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Mantenemos tu AppShell
            var window = new Window(new AppShell());
            return window;
        }
    }
}

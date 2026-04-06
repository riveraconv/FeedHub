using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using CommunityToolkit.Maui.Core;

namespace FeedHub_App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Mantenemos tu AppShell
            var window = new Window(new AppShell());
            return window;
        }
    }
}

namespace FeedHub_App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            var isDark = Preferences.Default.Get("DarkMode", false);
            UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
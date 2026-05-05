using FeedHub_Core.Services;
using FeedHub_Core.Utilities;
using FeedHub_App.ViewModels.News;

namespace FeedHub_App.Views.News
{
    
    [QueryProperty(nameof(Link), "link")]
    public partial class QuickViewPage : ContentPage, IQueryAttributable
    {
        private readonly QuickArticleService _articleService = new();
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public string? Link { get; set; }
        public string Source { get; set; }
        private bool _articleLoaded = false;
        private bool _quickViewAvailable = false;
        private bool _fullWebAvailable = false;

        public QuickViewPage(ILogger logger, QuickViewViewModel viewModel)
        {
            _logger = logger;
            _logger.Info("Worked");

            InitializeComponent();
            BindingContext = viewModel;

            //header for not seem a bot

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            };

            _httpClient = new HttpClient(handler);

            var ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ua);


        }
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("link", out var linkObj))
            {
                var newLink = Uri.UnescapeDataString(linkObj.ToString() ?? string.Empty);
                if (Link == newLink && _articleLoaded) return;

                Link = newLink;
                _articleLoaded = false; // Permitir recarga si la URL cambia
            }

            if (query.TryGetValue("source", out var sourceObj))
            {
                Source = Uri.UnescapeDataString(sourceObj.ToString() ?? "DESCONOCIDA");
                
                // IMPORTANTE: Aseguramos que se actualice en el hilo principal
                MainThread.BeginInvokeOnMainThread(() => {
                    SourceLabel.Text = Source.ToUpper();
                });
            }

            // Evitar doble carga si Shell reinyecta parámetros
            if (!_articleLoaded)
            {
                _articleLoaded = true;
                Task.Run(async () => await LoadArticleAsync());
            }
        }

        private async Task LoadArticleAsync()
        {
            if (string.IsNullOrEmpty(Link))
                return;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // Timeout razonable
                HttpResponseMessage? response = null;
                int maxRetries = 2;
                int attempt = 0;

                while (attempt <= maxRetries)
                {
                    attempt++;
                    response = await _httpClient.GetAsync(Link, cts.Token);

                    // Evitar loops de redirección infinitos
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        var redirectUri = response.Headers.Location;
                        if (redirectUri != null && redirectUri != response.RequestMessage.RequestUri)
                        {
                            Link = redirectUri.IsAbsoluteUri ? redirectUri.AbsoluteUri
                                                             : new Uri(new Uri(Link), redirectUri).AbsoluteUri;
                            continue; // Seguir con la nueva URL
                        }
                    }
                    break;
                }

                response.EnsureSuccessStatusCode();

                // Obtener HTML
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var charset = response.Content.Headers.ContentType?.CharSet;
                string html = _articleService.DecodeHtml(bytes, charset);

                // Parseo pesado en background
                var result = await Task.Run(() => _articleService.Extract(html));

                // Detectar contenido débil
                bool htmlIsWeak = string.IsNullOrWhiteSpace(result.Html)
                                  || result.Html.Length < 400
                                  || !result.Html.Contains("<p");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    

                    if (htmlIsWeak)
                    {
                        // SOLO FULL WEB MODE
                        ShowFullWeb();
                        InfoBanner.IsVisible = true;
                        InfoBanner.Text = "Fuiste redireccionado al sitio web original.";
                        FullWebView.Source = new UrlWebViewSource { Url = Link };
                    }
                    else
                    {
                        // SOLO QUICKVIEW MODE
                        ShowQuickView();

                        if (BindingContext is QuickViewViewModel vm)
                        {
                            var cleanText = System.Text.RegularExpressions.Regex.Replace(result.Html, "<.*?>", string.Empty);
                            vm.NewsContent = result.Text;
                        }
                        string finalHtml = ArticleHtmlContent(result.Html);

                        TitleLabel.Text = result.Title ?? "Sin título";

                        if (!string.IsNullOrEmpty(result.ImageUrl))
                            ArticleImage.Source = result.ImageUrl;

                        ArticleWebView.Source = new HtmlWebViewSource { Html = finalHtml };
                    }

                    ArticleWebView.Navigated += async (s, e) =>
                    {
                        if (Link.Contains("elconfidencial.com"))
                        {
                            string js = @"
                                    const removeOverlay = () => {
                                    const selectors = [
                                    '.Mrc_popin', '.modal-overlay', '.paywall', '.overlay',
                                    '.dscc__overlay', '#paywall', '#overlay', '.ec-ads-overlay'
                        ];
                            selectors.forEach(sel => {
                            document.querySelectorAll(sel).forEach(n => n.remove());
                        });
                        document.body.style.overflow = 'auto';
                        };
                        setTimeout(removeOverlay, 300);
                        removeOverlay();
                        ";

                            try
                            {
                                await ArticleWebView.EvaluateJavaScriptAsync(js);
                            }
                            catch { }
                        }
                    };


                });
            }
            catch (TaskCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Timeout", "The request took too long to respond.", "OK");
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", $"This article is not available right now: {ex.Message}", "OK");
                });
            }
        }

        private void ShowQuickView()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (QuickViewContainer == null) return;
                QuickViewContainer.IsVisible = true;
                FullWebView.IsVisible = false;

                ShareButton.IsVisible = true;
                SpeakButton.IsVisible = true;
                _quickViewAvailable = true;

                ToggleViewButton.IsVisible = true;
                ToggleViewButton.Text = "🌐";
                _logger.Info("Modo QuickView establecido correctamente");
            });
        }

        private void ShowFullWeb()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (FullWebView == null) return;
                QuickViewContainer.IsVisible = false;
                FullWebView.IsVisible = true;

                ShareButton.IsVisible = true;
                SpeakButton.IsVisible = true;
                _fullWebAvailable = true;

                ToggleViewButton.IsVisible = true;
                ToggleViewButton.Text = "📄";
                _logger.Info("Modo FullWeb establecido correctamente");
            });
        }
        private void OnToggleViewClicked(object sender, EventArgs e)
        {
            if (QuickViewContainer.IsVisible)
            {
                ShowFullWeb();
                if (FullWebView.Source == null || FullWebView.Source is UrlWebViewSource s && string.IsNullOrEmpty(s.Url))
                    FullWebView.Source = new UrlWebViewSource { Url = Link };
            }
            else
            {
                ShowQuickView();
            }
        }
        private void OnSourceClicked(object sender, EventArgs e)
        {
            if (BindingContext is QuickViewViewModel vm)
            {
                vm.StopSpeaking();
            }

            if (!string.IsNullOrEmpty(Link))
            {
                ShowFullWeb();
                FullWebView.Source = new UrlWebViewSource { Url = Link };
            }
        }
        private async void OnShareClicked(object sender, EventArgs e)
        {
            if (BindingContext is QuickViewViewModel vm)
            {
                vm.StopSpeaking();
            }

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = Link,
                Title = "Share the new"
            });
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateSystemBars();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (Application.Current != null)
                Application.Current.RequestedThemeChanged -= OnThemeChanged;

            if (BindingContext is QuickViewViewModel vm)
                vm.StopSpeaking();

        #if ANDROID
            var activity = Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(
                activity!.Window!, activity.Window!.DecorView);
            controller.AppearanceLightNavigationBars = false;
            controller.AppearanceLightStatusBars = false;
        #endif
        }

        private void UpdateSystemBars()
        {
        #if ANDROID
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var activity = Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(
                activity!.Window!, activity.Window!.DecorView);
            controller.AppearanceLightNavigationBars = !isDark;
            controller.AppearanceLightStatusBars = !isDark;
        #endif
        }

        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => UpdateSystemBars());
        }
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeChanged -= OnThemeChanged;
                Application.Current.RequestedThemeChanged += OnThemeChanged;
            }
            UpdateSystemBars();
        }

        public string ArticleHtmlContent(string ArticleContent)
        {
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var bgColor = isDark ? "#1E293B" : "#FFFFFF";
            var textColor = isDark ? "#F1F5F9" : "#1E293B";
            var linkColor = isDark ? "#60A5FA" : "#2563EB";
            var boldColor = isDark ? "#FFFFFF" : "#000000";

            return $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <style>
        html, body {{
            margin: 0;
            padding: 0;
            background-color: {bgColor};
            color: {textColor};
            font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            -webkit-text-size-adjust: 100%;
        }}
        body {{
            text-align: justify;
            text-justify: inter-word;
            padding: 15px 20px;
            word-break: break-word;
            line-height: 1.5;
            font-size: 14px;
        }}
        * {{
            color: {textColor} !important;
            background-color: transparent !important;
        }}
        html, body {{
            background-color: {bgColor} !important;
        }}
        a {{ color: {linkColor} !important; text-decoration: none; font-weight: bold; }}
        strong, b {{ color: {boldColor} !important; }}
        img {{
            max-width: 100%;
            height: auto;
            border-radius: 12px;
            margin: 15px 0;
            display: block;
        }}
        p {{ margin-bottom: 1.2em; }}
        ul, ol {{ padding-left: 20px; }}
        li {{ margin-bottom: 8px; }}
    </style>
</head>
<body>
    <div class='main-content'>
        {ArticleContent}
    </div>
</body>
</html>";
        }
    }
    
}



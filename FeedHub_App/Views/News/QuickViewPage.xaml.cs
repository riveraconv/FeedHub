using FeedHub_Core.Services;
using FeedHub_Core.Utilities;
using FeedHub_App.ViewModels.News;
using System.Net;

namespace FeedHub_App.Views.News
{

    [QueryProperty(nameof(Link), "link")]
    public partial class QuickViewPage : ContentPage, IQueryAttributable
    {
        private readonly QuickArticleCacheService _cacheService;
        private readonly QuickArticleService _articleService = new();
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        
        public string? Link { get; set; }
        public string Source { get; set; } = string.Empty;
        private bool _articleLoaded = false;
        private bool _quickViewAvailable = false;
        private bool _fullWebAvailable = false;


        public QuickViewPage(ILogger logger, QuickViewViewModel viewModel, QuickArticleCacheService cacheService)
        {
            _logger = logger;
            _logger.Info("Worked");

            _cacheService = cacheService;

            InitializeComponent();
            BindingContext = viewModel;

            QuickViewUnavailableMessage.Command = new Command(() =>
            OnSourceClicked(this, EventArgs.Empty));

            NoConnectionMessage.Command = new Command(() =>
            OnRetryClicked(this, EventArgs.Empty));

            ArticleWebView.Navigated += OnArticleNavigated;
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
        public static string NormalizeQueryValue(string? value, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var text = value.Trim();
            try
            {
                return Uri.UnescapeDataString(text);
            }
            catch
            {
                return text;
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            var newLink = NormalizeQueryValue(query.TryGetValue("link", out var linkObj) ? linkObj?.ToString() : null, string.Empty);
            if (!string.IsNullOrWhiteSpace(newLink))
            {
                if (Link == newLink && _articleLoaded) return;

                Link = newLink;
                _articleLoaded = false; // Permitir recarga si la URL cambia
            }
            else if (string.IsNullOrWhiteSpace(Link))
            {
                return;
            }

            if (query.TryGetValue("source", out var sourceObj))
            {
                Source = NormalizeQueryValue(sourceObj?.ToString(), "FUENTE");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SourceLabel.Text = Source.ToUpperInvariant();
                });
            }

            if (!_articleLoaded && !string.IsNullOrWhiteSpace(Link))
            {
                _articleLoaded = true;
                Task.Run(async () => await LoadArticleAsync());
            }
        }

        private async Task LoadArticleAsync()
        {
            if (string.IsNullOrWhiteSpace(Link))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ShowFullWeb();
                    InfoBanner.IsVisible = true;
                    InfoBannerText.Text = "La noticia no tiene enlace válido. Mostrando web original.";
                });
                return;
            }

            ShowLoading();

            NoConnectionPanel.IsVisible = false;
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ShowNoConnection();
                });

                return;
            }

            if (_cacheService.TryGet(Link, out var cached))
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ShowQuickView();

                        ArticleWebView.Source = new HtmlWebViewSource
                        {
                            Html = ArticleHtmlContent(cached.Html, cached.Title, cached.ImageUrl)
                        };

                        if (BindingContext is QuickViewViewModel vm)
                            vm.NewsContent = cached.Text;
                    });
                    _logger?.Info($"Artículo cargado desde caché: {Link}");

                    return; 
                }
                catch(Exception ex)
                {
                    _logger?.Info($"Artículo obtenido desde caché_ '{Link}' {ex.Message}");
                }
                
            }

            if (string.IsNullOrWhiteSpace(Link))
                return;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                HttpResponseMessage? response = null;
                int maxRetries = 2;
                int attempt = 0;

                while (attempt <= maxRetries)
                {
                    attempt++;

                    response = await _httpClient.GetAsync(Link, cts.Token);

                    // 🔴 1. BLOQUEO DIRECTO (403, 401, etc)
                    if (response.StatusCode == HttpStatusCode.Forbidden ||
                        response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ShowFullWeb();
                            InfoBanner.IsVisible = true;
                            InfoBannerText.Text = "Acceso al medio bloqueado. Abriendo web original..";
                            FullWebView.Source = new UrlWebViewSource { Url = Link };
                        });
                        return;
                    }

                    // 🔁 2. REDIRECCIONES CONTROLADAS
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        var redirectUri = response.Headers.Location;

                        if (redirectUri != null && redirectUri != response.RequestMessage.RequestUri)
                        {
                            Link = redirectUri.IsAbsoluteUri
                                ? redirectUri.AbsoluteUri
                                : new Uri(new Uri(Link), redirectUri).AbsoluteUri;

                            continue;
                        }
                    }

                    break;
                }

                if (response is null)
                {
                    _logger?.Warn($"Respuesta nula al cargar el artículo '{Link}'.");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ShowFullWeb();
                        InfoBanner.IsVisible = true;
                        InfoBannerText.Text = "No se pudo cargar el artículo. Mostrando web original.";
                        FullWebView.Source = new UrlWebViewSource { Url = Link };
                    });
                    return;
                }

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch
                {
                    _logger?.Warn($"HTTP no válido al cargar '{Link}' (status {(int?)response.StatusCode}).");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ShowFullWeb();
                        InfoBanner.IsVisible = true;
                        InfoBannerText.Text = "El contenido no está disponible. Mostrando web original.";
                        FullWebView.Source = new UrlWebViewSource { Url = Link };
                    });
                    return;
                }

                // 📥 3. OBTENER HTML
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var charset = response.Content.Headers.ContentType?.CharSet;
                string html = _articleService.DecodeHtml(bytes, charset);

                // ⚙️ 4. PARSEO Y GUARDADO EN CACHE
                var result = await Task.Run(() => _articleService.Extract(html));

                // 🧪 5. DETECTAR HTML INÚTIL
                bool htmlIsWeak = string.IsNullOrWhiteSpace(result.Html)
                                  || result.Html.Length < 400
                                  || !result.Html.Contains("<p");

                if (!htmlIsWeak)
                {
                    _cacheService.Save(Link, result);
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // 🔴 6. FALLBACK A WEB
                    if (htmlIsWeak)
                    {
                        _logger?.Warn($"QuickView no disponible para '{Link}'. HTML insuficiente");
                        ShowQuickViewUnavailable();
                        return;
                    }

                    // ✅ 7. QUICKVIEW
                    ShowQuickView();

                    if (BindingContext is QuickViewViewModel vm)
                    {
                        vm.NewsContent = result.Text;
                    }

                    string finalHtml = ArticleHtmlContent(result.Html, result.Title, result.ImageUrl);
                    ArticleWebView.Source = new HtmlWebViewSource { Html = finalHtml };

                });
            }
            catch (TaskCanceledException ex)
            {
                _logger?.Warn($"Timeout cargando el artículo '{Link}': {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    ShowFullWeb();
                    await Task.Delay(50);
                    InfoBanner.IsVisible = true;
                    InfoBannerText.Text = "La carga tardó demasiado. Mostrando web original.";
                    FullWebView.Source = new UrlWebViewSource { Url = Link };
                });
            }
            catch (HttpRequestException ex)
            {
                _logger?.Error($"Error HTTP cargando artículo '{Link}: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    ShowFullWeb();
                    await Task.Delay(50);
                    InfoBanner.IsVisible = true;
                    InfoBannerText.Text = "No se pudo procesar el artículo. Mostrando web original.";
                    FullWebView.Source = new UrlWebViewSource { Url = Link };
                });
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error inesperado cargando artículo '{Link}': {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ShowFullWeb();
                    InfoBanner.IsVisible = true;
                    InfoBannerText.Text = "Se produjo un error al cargar este artículo. Mostrando web original.";
                    if (!string.IsNullOrWhiteSpace(Link))
                        FullWebView.Source = new UrlWebViewSource { Url = Link };
                });
            }
        }
        private void ShowLoading()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                QuickViewContainer.IsVisible = true;
                FullWebView.IsVisible = false;

                ShareButton.IsVisible = false;
                SpeakButton.IsVisible = false;
                ToggleViewButton.IsVisible = false;

                ArticleWebView.Source = new HtmlWebViewSource
                {
                    Html = ArticleHtmlContent(@"
                        <div style='text-align:center; padding:40px 20px;'>
                            <p>Cargando contenido...</p>
                        </div>", "Cargando artículo...")
                };
            });
        }
        private void ShowQuickViewUnavailable()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _quickViewAvailable = false;

                QuickViewContainer.IsVisible = false;
                FullWebView.IsVisible = false;

                NoConnectionPanel.IsVisible = false;
                QuickViewUnavailablePanel.IsVisible = true;

                ShareButton.IsVisible = true;
                SpeakButton.IsVisible = false;

                ToggleViewButton.IsVisible = true;
                ToggleViewButton.Text = "🌐";

                InfoBanner.IsVisible = false;
            });
        }
        private async void OnRetryClicked(object sender, EventArgs e)
        {
            NoConnectionPanel.IsVisible = false;
            await LoadArticleAsync();
        }

        private void ShowQuickView()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (QuickViewContainer == null) return;

                QuickViewUnavailablePanel.IsVisible = false;
                NoConnectionPanel.IsVisible = false;

                QuickViewContainer.IsVisible = true;
                FullWebView.IsVisible = false;

                ShareButton.IsVisible = true;
                SpeakButton.IsVisible = true;

                _quickViewAvailable = true;

                ToggleViewButton.IsVisible = true;
                ToggleViewButton.Text = "🌐";

                InfoBanner.IsVisible = false;

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
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                ShowNoConnection();
                return;
            }

            if (QuickViewContainer.IsVisible)
            {
                ShowFullWeb();

                if (FullWebView.Source == null ||
                    FullWebView.Source is UrlWebViewSource s &&
                    string.IsNullOrEmpty(s.Url))
                {
                    FullWebView.Source = new UrlWebViewSource
                    {
                        Url = Link
                    };
                }

                return;
            }

            if (_quickViewAvailable)
            {
                ShowQuickView();
                return;
            }

            OnSourceClicked(this, EventArgs.Empty);
        }
        private void OnSourceClicked(object sender, EventArgs e)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                ShowNoConnection();
                return;
            }
            
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
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            ArticleWebView.Navigated -= OnArticleNavigated;


            if (BindingContext is QuickViewViewModel vm)
                vm.StopSpeaking();

        #if ANDROID
            App.UpdateSystemBars(Application.Current?.RequestedTheme ?? AppTheme.Dark);
        #endif
        }

        private void ShowNoConnection()
        {
            QuickViewContainer.IsVisible = false;
            FullWebView.IsVisible = false;
            QuickViewUnavailablePanel.IsVisible = false;

            InfoBanner.IsVisible = false;
            NoConnectionPanel.IsVisible = true;
        }

        public string ArticleHtmlContent(string articleContent, string? title = null, string? imageUrl = null)
        {
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var bgColor = isDark ? "#1E293B" : "#FFFFFF";
            var textColor = isDark ? "#F1F5F9" : "#1E293B";
            var linkColor = isDark ? "#60A5FA" : "#2563EB";
            var boldColor = isDark ? "#FFFFFF" : "#000000";
            var safeTitle = System.Net.WebUtility.HtmlEncode(title ?? "Sin título");
            var safeImageUrl = string.IsNullOrWhiteSpace(imageUrl)
                ? string.Empty
                : $"<img class='hero-image' src='{System.Net.WebUtility.HtmlEncode(imageUrl)}' />";

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
                        padding: 0 0 120px;
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
                    .hero {{ padding: 0 20px 16px; }}
                    .hero-image {{
                        width: calc(100% + 40px);
                        max-width: none;
                        height: 220px;
                        object-fit: cover;
                        margin: 0 -20px 24px;
                        border-radius: 0;
                    }}
                    .eyebrow {{
                        color: {linkColor} !important;
                        font-size: 11px;
                        font-weight: 700;
                        letter-spacing: 1.5px;
                        margin: 8px 5px 16px;
                    }}
                    h1 {{
                        color: {textColor} !important;
                        font-size: 20px;
                        line-height: 1.2;
                        margin: 0 5px 24px;
                        text-align: left;
                    }}
                    .divider {{
                        height: 2px;
                        background: {linkColor} !important;
                        opacity: .8;
                        margin: 0 0 12px;
                    }}
                    .main-content {{ padding: 0 20px; }}
                    p {{ margin-bottom: 1.2em; }}
                    ul, ol {{ padding-left: 20px; }}
                    li {{ margin-bottom: 8px; }}
                </style>
            </head>
            <body>
                <header class='hero'>
                    {safeImageUrl}
                    <div class='eyebrow'>FEEDHUB // VISTA RÁPIDA</div>
                    <h1>{safeTitle}</h1>
                    <div class='divider'></div>
                </header>
                <div class='main-content'>
                    {articleContent}
                    <div class='divider' style='margin-top:24px'></div>
                </div>
            </body>
            </html>";
        }
        private async void OnArticleNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (Link?.Contains("elconfidencial.com") == true)
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
                try { await ArticleWebView.EvaluateJavaScriptAsync(js); }
                catch (Exception ex)
                {
                    _logger?.Info($"No se pudo ejecutar el JavaScript para '{Link}': {ex.Message}");
                }
            }
        }
    }

}



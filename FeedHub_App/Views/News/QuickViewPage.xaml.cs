using FeedHub_Core.Services;
using Microsoft.Maui.Dispatching;
using System.Net.Http;
using System.Text;
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
        private bool _articleLoaded = false;

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

            Loaded += async (s, e) => await LoadArticleAsync();
        }
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("link", out var linkObj))
            {
                Link = Uri.UnescapeDataString(linkObj.ToString());
            }

            // Evitar doble carga si Shell reinyecta parámetros
            if (!_articleLoaded)
            {
                _articleLoaded = true;
                _ = LoadArticleAsync();
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
                        InfoBanner.Text = "You were redirected to the original website.";
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
        // 1. Verificamos que los componentes críticos existan
        if (QuickViewContainer == null || CloseButton == null) return;

        // 2. Visibilidad de contenedores
        QuickViewContainer.IsVisible = true;
        FullWebView.IsVisible = false;

        // 3. Estado de los botones
        SourceButton.IsVisible = true;
        ShareButton.IsVisible = true;
        SpeakButton.IsVisible = true;
        
        // 4. Reset del botón de cerrar
        CloseButton.IsVisible = true;
        CloseButton.Text = "✕";

        _logger.Info("Modo QuickView establecido correctamente");
    });
}

private void ShowFullWeb()
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        // 1. Verificamos nulos
        if (FullWebView == null || CloseButton == null) return;

        // 2. Visibilidad de contenedores
        QuickViewContainer.IsVisible = false;
        FullWebView.IsVisible = true;

        // 3. Ajuste de botones (Solo dejamos compartir y cerrar)
        SourceButton.IsVisible = false;
        ShareButton.IsVisible = true;
        SpeakButton.IsVisible = false;

        // 4. Lógica inteligente del botón de cerrar
        // Si hay un título cargado, significa que podemos "volver" a la Vista Rápida
        bool canGoBackToQuickView = !string.IsNullOrEmpty(TitleLabel?.Text) && 
                                    TitleLabel.Text != "Cargando título...";

        CloseButton.Text = canGoBackToQuickView ? "❮" : "✕";

        _logger.Info("Modo FullWeb establecido correctamente");
    });
}

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            if (FullWebView.IsVisible)
            {
                if (!string.IsNullOrEmpty(TitleLabel.Text) && TitleLabel.Text != "Cargando título...")
                {
                    ShowQuickView();
                    return;
                }
            }
            await Shell.Current.GoToAsync("..");
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
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is QuickViewViewModel vm)
            {
                vm.StopSpeaking();
            }
        }

            public string ArticleHtmlContent(string ArticleContent) => $@"
            <!DOCTYPE html>
            <html lang='es'>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
                <meta name='color-scheme' content='light dark'>
                <style>
                    /* 1. Configuramos el soporte para temas del sistema */
                    :root {{
                        color-scheme: light dark;
                        supports-color-scheme: light dark;
                    }}

                    /* 2. Estilos base para que el WebView sea transparente y use buena fuente */
                    html, body {{
                        background-color: transparent !important;
                        margin: 0;
                        padding: 0;
                        font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                        -webkit-text-size-adjust: 100%;
                    }}

                    body {{
                        padding: 5px;
                        line-height: 1.5;
                        font-size: 14px;
                    }}

                    /* 3. LA SOLUCIÓN NUCLEAR: Forzamos colores por tema */
                    
                    /* MODO OSCURO (Letras blancas sobre fondo oscuro de la app) */
                    @media (prefers-color-scheme: dark) {{
                        * {{
                            color: #F1F5F9 !important; /* Blanco suave */
                            background-color: transparent !important;
                        }}
                        a {{ color: #60A5FA !important; text-decoration: none; font-weight: bold; }}
                        strong, b {{ color: #FFFFFF !important; }}
                    }}

                    /* MODO CLARO (Letras oscuras sobre fondo claro de la app) */
                    @media (prefers-color-scheme: light) {{
                        * {{
                            color: #1E293B !important; /* Gris casi negro */
                            background-color: transparent !important;
                        }}
                        a {{ color: #2563EB !important; text-decoration: none; font-weight: bold; }}
                        strong, b {{ color: #000000 !important; }}
                    }}

                    /* 4. Ajustes de imágenes y otros elementos */
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



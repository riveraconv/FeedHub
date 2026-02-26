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
                    string template = await GetHtmlTemplateAsync();

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
                        string finalHtml = template.Replace("@ArticleContent@", result.Html);

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
            QuickViewContainer.IsVisible = true;
            FullWebView.IsVisible = false;
            SourceButton.IsVisible = true;
            ShareButton.IsVisible = true;
            SpeakButton.IsVisible = true;
            _logger.Info("Worked");
        }

        private void ShowFullWeb()
        {
            QuickViewContainer.IsVisible = false;
            FullWebView.IsVisible = true;
            SourceButton.IsVisible = false;
            ShareButton.IsVisible = true;
            SpeakButton.IsVisible = false;
            _logger.Info("Worked");
        }

        private async Task<string> GetHtmlTemplateAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("ArticleTemplate.html");
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading HTML template:{ex.Message}");
                return "<html>>body>@ArticleContent@</body></html>";
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
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
    }
}


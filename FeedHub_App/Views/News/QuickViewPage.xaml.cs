using FeedHub_Core.Services;
using Microsoft.Maui.Dispatching;
using System.Net.Http;
using System.Text;

namespace FeedHub_App.Views.News;

[QueryProperty(nameof(Link), "link")]
public partial class QuickViewPage : ContentPage
{
    private readonly QuickArticleService _articleService = new();
    private readonly HttpClient _httpClient;

    public string? Link { get; set; }

    public QuickViewPage()
    {
        InitializeComponent();

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

    private async Task LoadArticleAsync()
    {
        if (string.IsNullOrEmpty(Link))
            return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // Timeout razonable
            HttpResponseMessage response = null;
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
                TitleLabel.Text = result.Title ?? "Sin título";

                if (!string.IsNullOrEmpty(result.ImageUrl))
                    ArticleImage.Source = result.ImageUrl;

                if (htmlIsWeak)
                {
                    InfoBanner.IsVisible = true;
                    InfoBanner.Text = "You were redirected to the original website; " +
                                      "the content cannot be displayed in Quick View " +
                                      "due to reasons beyond the application's control.";

                    ArticleWebView.Source = new UrlWebViewSource { Url = Link };

                    await Task.Delay(6000);
                    InfoBanner.IsVisible = false;
                }
                else
                {
                    ArticleWebView.Source = new HtmlWebViewSource { Html = result.Html };
                }
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



    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}


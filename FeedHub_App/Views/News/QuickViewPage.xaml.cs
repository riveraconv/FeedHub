using FeedHub_Core.Services;
using Microsoft.Maui.Dispatching;
using System.Net.Http;
using System.Text;

namespace FeedHub_App.Views.News;

[QueryProperty(nameof(Link), "link")]
public partial class QuickViewPage : ContentPage
{
    private readonly QuickArticleService _articleService = new();
    private readonly HttpClient _httpClient = new();

    public string? Link { get; set; }

    public QuickViewPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadArticleAsync();
    }

    private async Task LoadArticleAsync()
    {
        if (string.IsNullOrEmpty(Link))
            return;

        try
        {
            using var response = await _httpClient.GetAsync(Link);
            response.EnsureSuccessStatusCode();

            // Leer bytes en bruto
            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Detectar charset desde header o meta
            var charset = response.Content.Headers.ContentType?.CharSet;
            string htmlProbe = System.Text.Encoding.UTF8.GetString(bytes.Take(4000).ToArray());

            if (string.IsNullOrWhiteSpace(charset))
            {
                var metaMatch = System.Text.RegularExpressions.Regex.Match(
                    htmlProbe,
                    @"<meta[^>]*charset\s*=\s*[""']?([\w\-]+)[""']?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (metaMatch.Success)
                    charset = metaMatch.Groups[1].Value;
            }

            charset = charset?.Trim().ToLowerInvariant();
            charset = charset switch
            {
                null or "" => "utf-8",
                "iso-8859-1" => "windows-1252",
                "iso-8859-15" => "windows-1252",
                "latin1" => "windows-1252",
                "us-ascii" => "utf-8",
                _ => charset
            };

            // Intento 1: usar charset detectado
            string html;
            try
            {
                var encoding = System.Text.Encoding.GetEncoding(charset);
                html = encoding.GetString(bytes);
            }
            catch
            {
                html = System.Text.Encoding.UTF8.GetString(bytes);
            }

            // 🔍 Intento 2: evaluar si la decodificación parece "rota"
            bool LooksCorrupted(string text)
            {
                int garbage = text.Count(c => c == '�');
                int wrongPairs = System.Text.RegularExpressions.Regex.Matches(text, "Ã.|Â.|\\?{2,}").Count;
                return garbage > 5 || wrongPairs > 2;
            }

            if (LooksCorrupted(html))
            {
                // Reintenta con las codificaciones más comunes
                var candidates = new[]
                {
                System.Text.Encoding.UTF8,
                System.Text.Encoding.GetEncoding("windows-1252"),
                System.Text.Encoding.GetEncoding("iso-8859-1"),
            };

                string best = html;
                int bestScore = int.MaxValue;

                foreach (var enc in candidates)
                {
                    var test = enc.GetString(bytes);
                    int score = test.Count(c => c == '�');
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = test;
                    }
                }

                html = best;
            }

            // Parseo
            var result = await Task.Run(() => _articleService.Extract(html));

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TitleLabel.Text = result.Title ?? "Sin título";

                if (!string.IsNullOrEmpty(result.ImageUrl))
                    ArticleImage.Source = result.ImageUrl;

                ArticleWebView.Source = new HtmlWebViewSource
                {
                    Html = result.Html ?? "<html><body><p>No se pudo mostrar el contenido.</p></body></html>"
                };
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Error", $"No se pudo cargar el artículo: {ex.Message}", "OK");
            });
        }
    }


    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}


using FeedHub_Core.Services;
using Microsoft.Maui.Dispatching;
using System.Net.Http;

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
        if (string.IsNullOrEmpty(Link)) return;

        try
        {
            // 🔹 Descarga HTML en el hilo principal (solo red, no bloquea)
            var html = await _httpClient.GetStringAsync(Link);

            // 🔹 Limpieza y parseo en background
            var result = await Task.Run(() => _articleService.Extract(html));

            // 🔹 Actualiza UI en MainThread
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














using Microsoft.Maui.Controls;

namespace FeedHub_App.Views.News;

public partial class NewsDetailPage : ContentPage
{
	public NewsDetailPage(string url)
	{
		InitializeComponent();
		NewsWebView.Source = url;
	}
}
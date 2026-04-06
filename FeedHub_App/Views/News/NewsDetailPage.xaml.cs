using FeedHub_Core.Models;

namespace FeedHub_App.Views.News;

public partial class NewsDetailPage : ContentPage, IQueryAttributable
{
    public NewsDetailPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("News", out var value) && value is NewsItem news)
        {
            BindingContext = news;
        }
    }
}




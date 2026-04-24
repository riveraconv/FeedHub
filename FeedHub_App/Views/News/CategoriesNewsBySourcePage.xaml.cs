#if ANDROID
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
#endif

using FeedHub_App.ViewModels.News;

namespace FeedHub_App.Views.News;

public partial class CategoriesNewsBySourcePage : ContentPage
{
	public CategoriesNewsBySourcePage(CategoriesBySourceViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
    private async void OnSourceClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is NewsSource source)
        {
            await ((CategoriesBySourceViewModel)BindingContext).SelectSourceCommand.ExecuteAsync(source.Id);
        }
    }
}
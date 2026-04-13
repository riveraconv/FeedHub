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
	protected override async void OnAppearing()
    {
        base.OnAppearing();
    
        this.Opacity = 0;
        this.TranslationX = 60;
            
    	await Task.Delay(50);
            
        await Task.WhenAll(
            this.FadeTo(1, 350, Easing.SinOut),
            this.TranslateTo(0, 0, 350, Easing.SinOut)
        );
    }
}
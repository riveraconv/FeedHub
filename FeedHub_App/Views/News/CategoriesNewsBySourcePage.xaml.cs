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
}
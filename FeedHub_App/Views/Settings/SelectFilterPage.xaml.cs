
using System.Diagnostics;
using FeedHub_App.ViewModels;
using FeedHub_Core.Models;

namespace FeedHub_App.Views.Settings;

public partial class SelectFilterPage : ContentPage
{
    private readonly FilterViewModel _viewModel;

    // Actualizamos el constructor para recibir el ViewModel
    public SelectFilterPage(FilterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
	private void OnFilterToggled(object sender, ToggledEventArgs e)
	{
		if (sender is Microsoft.Maui.Controls.Switch s && s.BindingContext is FilterItem item)
		{
			// Detectamos si es categoría o fuente por la visibilidad
			bool isCategory = _viewModel.ShowCategories; 
			System.Diagnostics.Debug.WriteLine($"#debug switch [{(isCategory ? "CATEGORÍA" : "FUENTE")}] '{item.Title}' (Code: '{item.Code}') → {(item.IsActive ? "ACTIVADO" : "DESACTIVADO")}");
			_viewModel.SavePreference(item, isCategory);
		}
	}
	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.LoadFiltersIfNeeded();
	}
}

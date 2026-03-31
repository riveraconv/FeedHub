using FeedHub_App.ViewModels.News;
using FeedHub_Core.Models;

namespace FeedHub_App.Views.News;

public partial class CategoryListPage : ContentPage
{
    private readonly CategoryListViewModel _viewModel;

    public CategoryListPage(CategoryListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
protected override bool OnBackButtonPressed()
{
    // Como ahora todas son "Modales" para el sistema, 
    // usamos Navigation.PopModalAsync() para cerrar.
    Dispatcher.Dispatch(async () => 
    {
        await Shell.Current.Navigation.PopModalAsync();
    });

    return true; // Bloquea el cierre de la App
}
}

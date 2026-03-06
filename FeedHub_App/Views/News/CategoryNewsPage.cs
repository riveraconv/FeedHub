using CommunityToolkit.Mvvm.Input;
using FeedHub_App.ViewModels.News;
using FeedHub_Core.Models;


namespace FeedHub_App.Views.News
{
    public partial class CategoryNewsPage : ContentPage
    {
        private readonly CategoryListViewModel _viewModel;

        public CategoryNewsPage(CategoryListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is NewsItem selected)
            {
                // Usamos el comando que ya existe en tu ViewModel
                _viewModel.OpenNewsCommand.Execute(selected);
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        // El comando de volver lo ponemos aquí en la vista para no ensuciar el VM
        // y que siempre funcione sin importar qué VM uses.
        [RelayCommand]
        public async Task GoBackToCategoryList()
        {
            // Como quieres ir a la lista de categorías:
            await Shell.Current.GoToAsync("CategoryListPage");
        }
    }
}


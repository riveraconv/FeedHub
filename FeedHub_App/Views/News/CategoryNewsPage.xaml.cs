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
                _viewModel.OpenNewsCommand.Execute(selected);
                ((CollectionView)sender).SelectedItem = null; // Limpiar selección
            }
        }
    }
}
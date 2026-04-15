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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if(BindingContext is CategoryListViewModel vm)
            {
                vm.SearchCompleted += OnSearchCompleted;
            }
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if(BindingContext is CategoryListViewModel vm)
            {
                vm.SearchCompleted -= OnSearchCompleted;
            }
        }
        private void OnSearchCompleted()
        {
            SearchBarControl.Unfocus();
        }
        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            SearchBarControl.Unfocus();
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
        private bool _isBarVisible = true;

        private async void OnNewsCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            var viewModel = BindingContext as CategoryListViewModel;
            if (viewModel == null) return;

            bool isAtEnd = e.LastVisibleItemIndex >= viewModel.Articles.Count - 1;

            if (isAtEnd && _isBarVisible)
            {
                _isBarVisible = false;
                await BottomSearchBar.TranslateTo(0, 100, 250, Easing.SinIn);
            }
            else if (!isAtEnd && !_isBarVisible)
            {
                _isBarVisible = true;
                await BottomSearchBar.TranslateTo(0, 0, 250, Easing.SinOut);
            }
        }
    }
}


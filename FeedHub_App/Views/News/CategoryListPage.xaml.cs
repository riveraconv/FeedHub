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
        protected override async void OnAppearing()
        {
            base.OnAppearing();
    
            // Resetea estado inicial cada vez
            this.Opacity = 0;
            this.TranslationX = 100;
            
            // Pequeña pausa para que Shell termine su propia transición
            await Task.Delay(100);
            
            await Task.WhenAll(
                this.FadeTo(1, 300, Easing.CubicOut),
                this.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }
    }

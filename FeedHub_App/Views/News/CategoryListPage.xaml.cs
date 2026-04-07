using FeedHub_App.ViewModels.News;

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
            this.TranslationX = 60;
            
            // Pequeña pausa para que Shell termine su propia transición
            await Task.Delay(150);
            
            await Task.WhenAll(
                this.FadeTo(1, 350, Easing.SinOut),
                this.TranslateTo(0, 0, 350, Easing.SinOut)
            );
        }
    }

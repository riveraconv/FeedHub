using FeedHub_App.ViewModels;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace FeedHub_App.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }
}

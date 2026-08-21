using System.Windows.Input;

namespace FeedHub_App.Controls;


public partial class StatusMessageView : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(
            nameof(Icon),
            typeof(string),
            typeof(StatusMessageView),
            string.Empty,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(StatusMessageView),
            string.Empty,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(StatusMessageView),
            string.Empty,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty ButtonTextProperty =
        BindableProperty.Create(
            nameof(ButtonText),
            typeof(string),
            typeof(StatusMessageView),
            string.Empty,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(StatusMessageView),
            propertyChanged: OnPropertyChanged);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public StatusMessageView()
    {
        InitializeComponent();

        UpdateVisuals();
    }

    private static void OnPropertyChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is StatusMessageView view)
        {
            view.UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (IconLabel == null)
            return;

        IconLabel.Text = Icon;
        TitleLabel.Text = Title;
        MessageLabel.Text = Message;
        ActionButton.Text = ButtonText;
        ActionButton.Command = Command;
    }
}
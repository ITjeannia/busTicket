using System.Windows.Input;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>
/// Illustration (ou icône dans un rond) + titre + explication + bouton facultatif.
/// Utilisé pour les listes vides et les erreurs de chargement.
/// </summary>
public class EmptyState : ContentView
{
    public static readonly BindableProperty IllustrationProperty = Ui.Prop<EmptyState, string?>(nameof(Illustration), null, e => e.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<EmptyState, string>(nameof(Icon), "", e => e.Refresh());
    public static readonly BindableProperty IconSizeProperty = Ui.Prop<EmptyState, double>(nameof(IconSize), 88.0, e => e.Refresh());
    public static readonly BindableProperty TitleProperty = Ui.Prop<EmptyState, string>(nameof(Title), "", e => e.Refresh());
    public static readonly BindableProperty MessageProperty = Ui.Prop<EmptyState, string>(nameof(Message), "", e => e.Refresh());
    public static readonly BindableProperty ActionTextProperty = Ui.Prop<EmptyState, string?>(nameof(ActionText), null, e => e.Refresh());
    public static readonly BindableProperty ActionCommandProperty = Ui.Prop<EmptyState, ICommand?>(nameof(ActionCommand), null, e => e.Refresh());
    public static readonly BindableProperty ActionVariantProperty = Ui.Prop<EmptyState, ButtonVariant>(nameof(ActionVariant), ButtonVariant.Primary, e => e.Refresh());

    /// <summary>Nom d'une image de Resources/Images (ex. « illus_no_trips.png »). Prioritaire sur l'icône.</summary>
    public string? Illustration { get => (string?)GetValue(IllustrationProperty); set => SetValue(IllustrationProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public ButtonVariant ActionVariant { get => (ButtonVariant)GetValue(ActionVariantProperty); set => SetValue(ActionVariantProperty, value); }

    private readonly Image _image = new() { HeightRequest = 150, HorizontalOptions = LayoutOptions.Center, Aspect = Aspect.AspectFit };
    private readonly Border _circle = new() { StrokeThickness = 0, BackgroundColor = AppColors.PrimarySoft, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _icon = Ui.Icon("", color: AppColors.Primary);
    private readonly Label _title = Ui.Text(size: AppFonts.Subtitle, font: AppFonts.Bold);
    private readonly Label _message = Ui.Text(size: AppFonts.Caption, color: AppColors.TextSecondary);
    private readonly AppButton _action = new() { Margin = new Thickness(0, AppSpacing.Sm, 0, 0) };
    private readonly VerticalStackLayout _stack;

    public EmptyState()
    {
        _circle.Content = _icon;
        _title.HorizontalTextAlignment = TextAlignment.Center;
        _message.HorizontalTextAlignment = TextAlignment.Center;
        _message.LineHeight = 1.25;
        _stack = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(AppSpacing.Lg, 0),
            VerticalOptions = LayoutOptions.Center,
            Children = { _image, _circle, _title, _message, _action },
        };
        Content = _stack;
        Refresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Petite entrée animée chaque fois que l'état devient visible.
        if (propertyName == nameof(IsVisible) && IsVisible)
        {
            _stack.Opacity = 0;
            _stack.TranslationY = 16;
            _ = _stack.FadeToAsync(1, AppMotion.Slow, Easing.CubicOut);
            _ = _stack.TranslateToAsync(0, 0, AppMotion.Slow, Easing.CubicOut);
        }
    }

    private void Refresh()
    {
        var hasImage = !string.IsNullOrEmpty(Illustration);
        _image.Source = hasImage ? Illustration : null;
        _image.IsVisible = hasImage;
        _circle.IsVisible = !hasImage;
        _circle.WidthRequest = _circle.HeightRequest = IconSize;
        _circle.StrokeShape = Ui.Rounded(IconSize / 2);
        _icon.Text = Icon;
        _icon.FontSize = IconSize * 0.42;
        _title.Text = Title;
        _message.Text = Message;
        _action.Text = ActionText ?? "";
        _action.Command = ActionCommand;
        _action.Variant = ActionVariant;
        _action.IsVisible = ActionText is not null;
    }
}

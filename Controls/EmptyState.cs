using System.Windows.Input;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>
/// Icône dans un rond gris + titre + explication + bouton facultatif.
/// Utilisé pour les listes vides et les erreurs de chargement.
/// </summary>
public class EmptyState : ContentView
{
    public static readonly BindableProperty IconProperty = Ui.Prop<EmptyState, string>(nameof(Icon), "", e => e.Refresh());
    public static readonly BindableProperty IconSizeProperty = Ui.Prop<EmptyState, double>(nameof(IconSize), 96.0, e => e.Refresh());
    public static readonly BindableProperty TitleProperty = Ui.Prop<EmptyState, string>(nameof(Title), "", e => e.Refresh());
    public static readonly BindableProperty MessageProperty = Ui.Prop<EmptyState, string>(nameof(Message), "", e => e.Refresh());
    public static readonly BindableProperty ActionTextProperty = Ui.Prop<EmptyState, string?>(nameof(ActionText), null, e => e.Refresh());
    public static readonly BindableProperty ActionCommandProperty = Ui.Prop<EmptyState, ICommand?>(nameof(ActionCommand), null, e => e.Refresh());
    public static readonly BindableProperty ActionVariantProperty = Ui.Prop<EmptyState, ButtonVariant>(nameof(ActionVariant), ButtonVariant.Primary, e => e.Refresh());

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public ButtonVariant ActionVariant { get => (ButtonVariant)GetValue(ActionVariantProperty); set => SetValue(ActionVariantProperty, value); }

    private readonly Border _circle = new() { StrokeThickness = 0, BackgroundColor = AppColors.SurfaceMuted, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _icon = Ui.Icon("");
    private readonly Label _title = Ui.Text(size: AppFonts.Subtitle, font: AppFonts.Medium);
    private readonly Label _message = Ui.Text(size: AppFonts.Body, color: AppColors.TextSecondary);
    private readonly AppButton _action = new() { Margin = new Thickness(0, AppSpacing.Xs, 0, 0) };

    public EmptyState()
    {
        _circle.Content = _icon;
        _title.HorizontalTextAlignment = TextAlignment.Center;
        _message.HorizontalTextAlignment = TextAlignment.Center;
        Content = new VerticalStackLayout
        {
            Spacing = 12,
            Padding = new Thickness(AppSpacing.Md, 0),
            VerticalOptions = LayoutOptions.Center,
            Children = { _circle, _title, _message, _action },
        };
        Refresh();
    }

    private void Refresh()
    {
        _circle.WidthRequest = _circle.HeightRequest = IconSize;
        _circle.StrokeShape = Ui.Rounded(IconSize / 2);
        _icon.Text = Icon;
        _icon.FontSize = IconSize * 0.4;
        _title.Text = Title;
        _message.Text = Message;
        _action.Text = ActionText ?? "";
        _action.Command = ActionCommand;
        _action.Variant = ActionVariant;
        _action.IsVisible = ActionText is not null;
    }
}

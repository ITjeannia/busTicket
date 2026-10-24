using System.Windows.Input;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Primary = bleu plein ; Outlined = contour bleu ; Light = blanc (sur fond bleu) ; Ghost = texte seul.</summary>
public enum ButtonVariant { Primary, Outlined, Light, Ghost }

/// <summary>
/// Bouton pleine largeur de l'application.
/// - IsEnabled = false → grisé.
/// - IsLoading → indicateur + LoadingText ; le bouton garde sa couleur mais n'est plus cliquable.
/// - Icon → glyphe Material à gauche du texte.
/// </summary>
public class AppButton : ContentView
{
    public static readonly BindableProperty TextProperty = Ui.Prop<AppButton, string>(nameof(Text), "", b => b.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<AppButton, string?>(nameof(Icon), null, b => b.Refresh());
    public static readonly BindableProperty LoadingTextProperty = Ui.Prop<AppButton, string?>(nameof(LoadingText), null, b => b.Refresh());
    public static readonly BindableProperty IsLoadingProperty = Ui.Prop<AppButton, bool>(nameof(IsLoading), false, b => b.Refresh());
    public static readonly BindableProperty VariantProperty = Ui.Prop<AppButton, ButtonVariant>(nameof(Variant), ButtonVariant.Primary, b => b.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<AppButton, ICommand?>(nameof(Command));
    public static readonly BindableProperty CommandParameterProperty = Ui.Prop<AppButton, object?>(nameof(CommandParameter));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? LoadingText { get => (string?)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public event EventHandler? Clicked;

    private readonly Border _box = new() { StrokeShape = Ui.Rounded(AppSpacing.RadiusSm), HeightRequest = AppSpacing.ControlHeight, Padding = new Thickness(AppSpacing.Md, 0) };
    private readonly ActivityIndicator _spinner = new() { WidthRequest = 20, HeightRequest = 20, IsRunning = true };
    private readonly Label _icon = Ui.Icon("", 20);
    private readonly Label _label = Ui.Text(font: AppFonts.Medium);

    public AppButton()
    {
        _label.VerticalOptions = LayoutOptions.Center;
        _label.LineBreakMode = LineBreakMode.TailTruncation;
        _box.Content = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { _spinner, _icon, _label },
        };
        _box.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(OnTapped) });
        Content = _box;
        Refresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Refresh();
    }

    private async void OnTapped()
    {
        if (!IsEnabled || IsLoading) return;
        await Motion.PressAsync(_box);
        Clicked?.Invoke(this, EventArgs.Empty);
        if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);
    }

    private void Refresh()
    {
        var enabled = IsEnabled || IsLoading;
        var (background, stroke, foreground) = Variant switch
        {
            _ when !enabled => (AppColors.SurfaceMuted, Colors.Transparent, AppColors.TextDisabled),
            ButtonVariant.Outlined => (AppColors.Surface, AppColors.Primary, AppColors.Primary),
            ButtonVariant.Light => (AppColors.Surface, Colors.Transparent, AppColors.Primary),
            ButtonVariant.Ghost => (Colors.Transparent, Colors.Transparent, AppColors.Primary),
            _ => (AppColors.Primary, Colors.Transparent, AppColors.TextOnPrimary),
        };

        _box.BackgroundColor = background;
        _box.Stroke = stroke;
        _box.StrokeThickness = Variant == ButtonVariant.Outlined && enabled ? 1.5 : 0;
        _box.Shadow = Variant == ButtonVariant.Primary && enabled
            ? new Shadow { Brush = AppColors.Primary, Opacity = 0.25f, Radius = 10, Offset = new Point(0, 4) }
            : null!;

        _label.TextColor = foreground;
        _label.Text = IsLoading ? LoadingText ?? Text : Text;
        _label.IsVisible = !string.IsNullOrEmpty(_label.Text);
        _icon.TextColor = foreground;
        _icon.Text = Icon ?? "";
        _icon.IsVisible = Icon is not null && !IsLoading;
        _spinner.Color = foreground;
        _spinner.IsVisible = IsLoading;
        SemanticProperties.SetDescription(_box, _label.Text);
    }
}

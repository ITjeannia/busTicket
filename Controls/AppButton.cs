using System.Windows.Input;
using BusTicket.Theme;

namespace BusTicket.Controls;

public enum ButtonVariant { Primary, Outlined }

/// <summary>
/// Bouton pleine largeur de l'application.
/// - IsEnabled = false → grisé.
/// - IsLoading → indicateur + LoadingText ; le bouton reste bleu mais n'est plus cliquable.
/// </summary>
public class AppButton : ContentView
{
    public static readonly BindableProperty TextProperty = Ui.Prop<AppButton, string>(nameof(Text), "", b => b.Refresh());
    public static readonly BindableProperty LoadingTextProperty = Ui.Prop<AppButton, string?>(nameof(LoadingText), null, b => b.Refresh());
    public static readonly BindableProperty IsLoadingProperty = Ui.Prop<AppButton, bool>(nameof(IsLoading), false, b => b.Refresh());
    public static readonly BindableProperty VariantProperty = Ui.Prop<AppButton, ButtonVariant>(nameof(Variant), ButtonVariant.Primary, b => b.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<AppButton, ICommand?>(nameof(Command));
    public static readonly BindableProperty CommandParameterProperty = Ui.Prop<AppButton, object?>(nameof(CommandParameter));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? LoadingText { get => (string?)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private readonly Border _box = new() { StrokeShape = Ui.Rounded(AppSpacing.RadiusMd), HeightRequest = AppSpacing.ControlHeight, Padding = new Thickness(AppSpacing.Md, 0) };
    private readonly ActivityIndicator _spinner = new() { WidthRequest = 20, HeightRequest = 20, Color = AppColors.TextOnPrimary, IsRunning = true };
    private readonly Label _label = Ui.Text(font: AppFonts.Medium);

    public AppButton()
    {
        _label.VerticalOptions = LayoutOptions.Center;
        _box.Content = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { _spinner, _label },
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

    private void OnTapped()
    {
        if (!IsEnabled || IsLoading) return;
        if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);
    }

    private void Refresh()
    {
        var outlined = Variant == ButtonVariant.Outlined;
        var enabled = IsEnabled || IsLoading;

        _box.BackgroundColor = outlined ? Colors.Transparent : enabled ? AppColors.Primary : AppColors.SurfaceMuted;
        _box.Stroke = outlined ? AppColors.Primary : Colors.Transparent;
        _box.StrokeThickness = outlined ? 1.5 : 0;
        _label.TextColor = outlined ? AppColors.Primary : enabled ? AppColors.TextOnPrimary : AppColors.TextDisabled;
        _label.Text = IsLoading ? LoadingText ?? Text : Text;
        _spinner.IsVisible = IsLoading;
        SemanticProperties.SetDescription(_box, _label.Text);
    }
}

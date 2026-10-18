using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>
/// Champ de saisie avec libellé au-dessus et message en dessous.
/// Gère seul ses couleurs : actif (bleu), erreur (rouge + icône), désactivé (gris).
/// </summary>
public class AppTextField : ContentView
{
    public static readonly BindableProperty LabelProperty = Ui.Prop<AppTextField, string>(nameof(Label), "", f => f.Refresh());
    public static readonly BindableProperty TextProperty = Ui.Prop<AppTextField, string>(nameof(Text), "", mode: BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = Ui.Prop<AppTextField, string>(nameof(Placeholder), "", f => f.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<AppTextField, string?>(nameof(Icon), null, f => f.Refresh());
    public static readonly BindableProperty ErrorTextProperty = Ui.Prop<AppTextField, string?>(nameof(ErrorText), null, f => f.Refresh());
    public static readonly BindableProperty HelperTextProperty = Ui.Prop<AppTextField, string?>(nameof(HelperText), null, f => f.Refresh());
    public static readonly BindableProperty IsPasswordProperty = Ui.Prop<AppTextField, bool>(nameof(IsPassword), false, f => f.Refresh());
    public static readonly BindableProperty KeyboardProperty = Ui.Prop<AppTextField, Keyboard>(nameof(Keyboard), Keyboard.Default, f => f.Refresh());

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    /// <summary>Message d'erreur affiché sous le champ (null = pas d'erreur).</summary>
    public string? ErrorText { get => (string?)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    /// <summary>Aide affichée sous le champ quand il n'y a pas d'erreur.</summary>
    public string? HelperText { get => (string?)GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }

    private readonly Label _label = Ui.Text(size: AppFonts.Caption, font: AppFonts.Medium);
    private readonly Label _icon = Ui.Icon("");
    private readonly Label _errorIcon = Ui.Icon(MaterialIcons.Error, color: AppColors.Error);
    private readonly Entry _entry = new() { FontFamily = AppFonts.Regular, FontSize = AppFonts.Body, VerticalOptions = LayoutOptions.Center };
    private readonly Border _box = new() { StrokeShape = Ui.Rounded(AppSpacing.RadiusSm), HeightRequest = AppSpacing.ControlHeight, Padding = new Thickness(12, 0) };
    private readonly Label _message = Ui.Text(size: AppFonts.Caption);

    public AppTextField()
    {
        _entry.SetBinding(Entry.TextProperty, new Binding(nameof(Text), BindingMode.TwoWay, source: this));
        _entry.Focused += (_, _) => Refresh();
        _entry.Unfocused += (_, _) => Refresh();

        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 12,
        };
        row.Add(_icon, 0);
        row.Add(_entry, 1);
        row.Add(_errorIcon, 2);
        _box.Content = row;

        Content = new VerticalStackLayout { Spacing = 6, Children = { _label, _box, _message } };
        Refresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Refresh();
    }

    private void Refresh()
    {
        var hasError = ErrorText is not null;
        var color = !IsEnabled ? AppColors.TextDisabled
                  : hasError ? AppColors.Error
                  : _entry.IsFocused ? AppColors.Primary
                  : AppColors.TextPrimary;

        _label.Text = Label;
        _label.TextColor = color;

        _icon.Text = Icon ?? "";
        _icon.IsVisible = Icon is not null;
        _icon.TextColor = hasError || _entry.IsFocused ? color : AppColors.TextSecondary;
        _errorIcon.IsVisible = hasError;

        _entry.Placeholder = Placeholder;
        _entry.PlaceholderColor = AppColors.TextSecondary;
        _entry.IsPassword = IsPassword;
        _entry.Keyboard = Keyboard;
        _entry.IsEnabled = IsEnabled;
        _entry.TextColor = IsEnabled ? AppColors.TextPrimary : AppColors.TextDisabled;

        _box.Stroke = hasError || _entry.IsFocused ? color : AppColors.Border;
        _box.StrokeThickness = hasError || _entry.IsFocused ? 2 : 1;
        _box.BackgroundColor = IsEnabled ? AppColors.Surface : AppColors.SurfaceMuted;

        _message.Text = ErrorText ?? HelperText;
        _message.TextColor = hasError ? AppColors.Error : AppColors.TextSecondary;
        _message.IsVisible = _message.Text is not null;

        SemanticProperties.SetDescription(_entry, Label);
    }
}

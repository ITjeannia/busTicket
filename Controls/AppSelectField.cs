using System.Windows.Input;
using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Champ non éditable qui exécute <see cref="Command"/> au toucher (ville, passagers…).</summary>
public class AppSelectField : ContentView
{
    public static readonly BindableProperty LabelProperty = Ui.Prop<AppSelectField, string>(nameof(Label), "", f => f.Refresh());
    public static readonly BindableProperty ValueProperty = Ui.Prop<AppSelectField, string?>(nameof(Value), null, f => f.Refresh());
    public static readonly BindableProperty PlaceholderProperty = Ui.Prop<AppSelectField, string>(nameof(Placeholder), "Choisir", f => f.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<AppSelectField, string>(nameof(Icon), "", f => f.Refresh());
    public static readonly BindableProperty ErrorTextProperty = Ui.Prop<AppSelectField, string?>(nameof(ErrorText), null, f => f.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<AppSelectField, ICommand?>(nameof(Command));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? ErrorText { get => (string?)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private readonly Label _label = Ui.Text(size: AppFonts.Caption, font: AppFonts.Medium);
    private readonly Label _icon = Ui.Icon("");
    private readonly Label _value = Ui.Text();
    private readonly Label _errorIcon = Ui.Icon(MaterialIcons.Error, color: AppColors.Error);
    private readonly Border _box = new() { StrokeShape = Ui.Rounded(AppSpacing.RadiusSm), HeightRequest = AppSpacing.ControlHeight, Padding = new Thickness(12, 0), BackgroundColor = AppColors.Surface };
    private readonly Label _message = Ui.Text(size: AppFonts.Caption, color: AppColors.Error);

    public AppSelectField()
    {
        _value.VerticalOptions = LayoutOptions.Center;
        _value.LineBreakMode = LineBreakMode.TailTruncation;

        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 12,
        };
        row.Add(_icon, 0);
        row.Add(_value, 1);
        row.Add(_errorIcon, 2);
        _box.Content = row;
        _box.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { if (Command?.CanExecute(null) == true) Command.Execute(null); }),
        });

        Content = new VerticalStackLayout { Spacing = 6, Children = { _label, _box, _message } };
        Refresh();
    }

    private void Refresh()
    {
        var hasError = ErrorText is not null;
        _label.Text = Label;
        _label.TextColor = hasError ? AppColors.Error : AppColors.TextPrimary;
        _icon.Text = Icon;
        _icon.TextColor = hasError ? AppColors.Error : AppColors.TextSecondary;
        _value.Text = Value ?? Placeholder;
        _value.TextColor = Value is null ? AppColors.TextSecondary : AppColors.TextPrimary;
        _errorIcon.IsVisible = hasError;
        _box.Stroke = hasError ? AppColors.Error : AppColors.Border;
        _box.StrokeThickness = hasError ? 2 : 1;
        _message.Text = ErrorText;
        _message.IsVisible = hasError;
        SemanticProperties.SetDescription(_box, $"{Label} : {Value ?? Placeholder}");
    }
}

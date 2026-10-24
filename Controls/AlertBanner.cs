using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

public enum AlertKind { Error, Info, Neutral, Success, Warning }

/// <summary>
/// Bandeau de message avec liseré coloré à gauche : rouge (Error), bleu (Info), gris (Neutral),
/// vert (Success), orange (Warning). Masqué automatiquement quand <see cref="Message"/> est vide.
/// </summary>
public class AlertBanner : ContentView
{
    public static readonly BindableProperty MessageProperty = Ui.Prop<AlertBanner, string?>(nameof(Message), null, b => b.Refresh());
    public static readonly BindableProperty KindProperty = Ui.Prop<AlertBanner, AlertKind>(nameof(Kind), AlertKind.Error, b => b.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<AlertBanner, string?>(nameof(Icon), null, b => b.Refresh());

    public string? Message { get => (string?)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public AlertKind Kind { get => (AlertKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    /// <summary>Icône personnalisée (sinon celle du type).</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    private readonly Label _icon = Ui.Icon("", 20);
    private readonly Label _message = Ui.Text(size: AppFonts.Caption);
    private readonly BoxView _accent = new() { WidthRequest = 4 };
    private readonly Border _box = new() { StrokeThickness = 0, StrokeShape = Ui.Rounded(AppSpacing.RadiusSm), Padding = 0 };

    public AlertBanner()
    {
        _icon.VerticalOptions = LayoutOptions.Start;
        _message.LineHeight = 1.2;
        _message.VerticalOptions = LayoutOptions.Center;

        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star)],
            ColumnSpacing = 12,
        };
        row.Add(_accent, 0);
        row.Add(new ContentView { Content = _icon, Padding = new Thickness(0, 12) }, 1);
        row.Add(new ContentView { Content = _message, Padding = new Thickness(0, 12, 14, 12) }, 2);
        _box.Content = row;
        Content = _box;
        Refresh();
    }

    private void Refresh()
    {
        var (background, color, glyph) = Kind switch
        {
            AlertKind.Error => (AppColors.ErrorSoft, AppColors.Error, MaterialIcons.Error),
            AlertKind.Info => (AppColors.PrimarySoft, AppColors.Primary, MaterialIcons.Info),
            AlertKind.Success => (AppColors.SuccessSoft, AppColors.Success, MaterialIcons.CheckCircle),
            AlertKind.Warning => (AppColors.WarningSoft, AppColors.Warning, MaterialIcons.CloudOff),
            _ => (AppColors.SurfaceMuted, AppColors.TextSecondary, MaterialIcons.Info),
        };
        _box.BackgroundColor = background;
        _accent.Color = color;
        _icon.TextColor = color;
        _icon.Text = Icon ?? glyph;
        _message.Text = Message;
        IsVisible = !string.IsNullOrEmpty(Message);
    }
}

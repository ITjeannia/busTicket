using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

public enum AlertKind { Error, Info, Neutral }

/// <summary>
/// Bandeau de message : rouge (Error), bleu (Info) ou gris (Neutral, ex. hors ligne).
/// Masqué automatiquement quand <see cref="Message"/> est vide.
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
    private readonly Border _box = new() { StrokeThickness = 0, StrokeShape = Ui.Rounded(AppSpacing.RadiusSm), Padding = AppSpacing.Md };

    public AlertBanner()
    {
        _icon.VerticalOptions = LayoutOptions.Start;
        _message.LineHeight = 1.2;
        var row = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)], ColumnSpacing = 12 };
        row.Add(_icon, 0);
        row.Add(_message, 1);
        _box.Content = row;
        Content = _box;
        Refresh();
    }

    private void Refresh()
    {
        var (background, iconColor, glyph) = Kind switch
        {
            AlertKind.Error => (AppColors.ErrorSoft, AppColors.Error, MaterialIcons.Error),
            AlertKind.Info => (AppColors.PrimarySoft, AppColors.Primary, MaterialIcons.Info),
            _ => (AppColors.SurfaceMuted, AppColors.TextSecondary, MaterialIcons.Download),
        };
        _box.BackgroundColor = background;
        _icon.TextColor = iconColor;
        _icon.Text = Icon ?? glyph;
        _message.Text = Message;
        IsVisible = !string.IsNullOrEmpty(Message);
    }
}

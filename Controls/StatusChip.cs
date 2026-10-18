using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

public enum ChipKind { Valid, Expired, Neutral, Full }

/// <summary>Petite étiquette arrondie : « Valide », « Expiré », « Complet », « Disponible hors ligne »…</summary>
public class StatusChip : ContentView
{
    public static readonly BindableProperty KindProperty = Ui.Prop<StatusChip, ChipKind>(nameof(Kind), ChipKind.Valid, c => c.Refresh());
    public static readonly BindableProperty TextProperty = Ui.Prop<StatusChip, string?>(nameof(Text), null, c => c.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<StatusChip, string?>(nameof(Icon), null, c => c.Refresh());

    public ChipKind Kind { get => (ChipKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    /// <summary>Texte personnalisé (sinon celui du type).</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    private readonly Label _icon = Ui.Icon("", 16);
    private readonly Label _text = Ui.Text(size: AppFonts.Caption, font: AppFonts.Medium);
    private readonly Border _box = new() { StrokeThickness = 0, StrokeShape = Ui.Rounded(AppSpacing.RadiusPill), Padding = new Thickness(10, 5) };

    public StatusChip()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _box.Content = new HorizontalStackLayout { Spacing = AppSpacing.Xs, Children = { _icon, _text } };
        Content = _box;
        Refresh();
    }

    private void Refresh()
    {
        var (fg, bg, text, glyph) = Kind switch
        {
            ChipKind.Valid => (AppColors.Success, AppColors.SuccessSoft, "Valide", MaterialIcons.CheckCircle),
            ChipKind.Expired => (AppColors.Error, AppColors.ErrorSoft, "Expiré", MaterialIcons.Error),
            ChipKind.Full => (AppColors.TextPrimary, AppColors.Disabled, "Complet", (string?)null),
            _ => (AppColors.TextSecondary, AppColors.SurfaceMuted, "", (string?)null),
        };
        _box.BackgroundColor = bg;
        _text.TextColor = fg;
        _text.Text = Text ?? text;
        _icon.TextColor = fg;
        _icon.Text = Icon ?? glyph ?? "";
        _icon.IsVisible = _icon.Text.Length > 0;
    }
}

using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

public enum ChipKind { Valid, Expired, Neutral, Full, Boarded, Info, Warning }

/// <summary>Petite étiquette : « Valide », « Expiré », « Embarqué », « Complet », « VIP »…</summary>
public class StatusChip : ContentView
{
    public static readonly BindableProperty KindProperty = Ui.Prop<StatusChip, ChipKind>(nameof(Kind), ChipKind.Valid, c => c.Refresh());
    public static readonly BindableProperty TextProperty = Ui.Prop<StatusChip, string?>(nameof(Text), null, c => c.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<StatusChip, string?>(nameof(Icon), null, c => c.Refresh());

    public ChipKind Kind { get => (ChipKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    /// <summary>Texte personnalisé (sinon celui du type).</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    private readonly Label _icon = Ui.Icon("", 14);
    private readonly Label _text = Ui.Text(size: AppFonts.Small, font: AppFonts.Medium);
    private readonly Border _box = new() { StrokeThickness = 0, StrokeShape = Ui.Rounded(AppSpacing.RadiusXs), Padding = new Thickness(8, 4) };

    public StatusChip()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _text.VerticalOptions = LayoutOptions.Center;
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
            ChipKind.Boarded => (AppColors.Primary, AppColors.PrimarySoft, "Embarqué", MaterialIcons.DoneAll),
            ChipKind.Full => (AppColors.TextSecondary, AppColors.SurfaceMuted, "Complet", (string?)null),
            ChipKind.Info => (AppColors.Primary, AppColors.PrimarySoft, "", (string?)null),
            ChipKind.Warning => (AppColors.Warning, AppColors.WarningSoft, "", (string?)null),
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

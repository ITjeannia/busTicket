using BusTicket.Theme;
using Microsoft.Maui.Controls.Shapes;

namespace BusTicket.Controls;

/// <summary>Petites fabriques partagées par les contrôles écrits en C#.</summary>
internal static class Ui
{
    public static Label Text(string? text = null, double size = AppFonts.Body, Color? color = null,
                             string font = AppFonts.Regular) => new()
    {
        Text = text,
        FontSize = size,
        FontFamily = font,
        TextColor = color ?? AppColors.TextPrimary,
    };

    public static Label Icon(string glyph, double size = 22, Color? color = null) => new()
    {
        Text = glyph,
        FontFamily = AppFonts.Icons,
        FontSize = size,
        TextColor = color ?? AppColors.TextSecondary,
        VerticalOptions = LayoutOptions.Center,
        HorizontalOptions = LayoutOptions.Center,
    };

    public static RoundRectangle Rounded(double radius) => new() { CornerRadius = new CornerRadius(radius) };

    /// <summary>Crée une BindableProperty dont le changement appelle <paramref name="onChanged"/>.</summary>
    public static BindableProperty Prop<TOwner, TValue>(string name, TValue defaultValue = default!,
        Action<TOwner>? onChanged = null, BindingMode mode = BindingMode.OneWay)
        where TOwner : BindableObject =>
        BindableProperty.Create(name, typeof(TValue), typeof(TOwner), defaultValue, mode,
            propertyChanged: onChanged is null ? null : (b, _, _) => onChanged((TOwner)b));
}

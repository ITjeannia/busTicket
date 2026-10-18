using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Carte « squelette » affichée pendant un chargement.</summary>
public class SkeletonCard : ContentView
{
    public SkeletonCard()
    {
        static BoxView Bar(double widthRatio, double height) => new()
        {
            HeightRequest = height,
            CornerRadius = AppSpacing.RadiusSm,
            Color = AppColors.SurfaceMuted,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = 300 * widthRatio,
        };

        Content = new Border
        {
            Stroke = AppColors.Border,
            StrokeShape = Ui.Rounded(AppSpacing.RadiusMd),
            Padding = AppSpacing.Md,
            Content = new VerticalStackLayout
            {
                Spacing = AppSpacing.Md,
                Children = { Bar(0.65, 20), Bar(0.85, 14), Bar(0.45, 14) },
            },
        };
    }
}

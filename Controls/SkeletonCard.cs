using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Carte « squelette » qui pulse pendant un chargement.</summary>
public class SkeletonCard : ContentView
{
    public SkeletonCard()
    {
        static BoxView Bar(double width, double height) => new()
        {
            HeightRequest = height,
            WidthRequest = width,
            CornerRadius = AppSpacing.RadiusXs,
            Color = AppColors.SurfaceMuted,
            HorizontalOptions = LayoutOptions.Start,
        };

        var top = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        top.Add(Bar(150, 22), 0);
        top.Add(Bar(80, 22), 1);

        var body = new Border
        {
            Stroke = AppColors.Divider,
            StrokeShape = Ui.Rounded(AppSpacing.RadiusMd),
            BackgroundColor = AppColors.Surface,
            Padding = AppSpacing.Md,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { top, Bar(220, 12), Bar(130, 12) },
            },
        };
        Content = body;

        Loaded += (_, _) => Motion.Pulse(body, 0.5, 1100);
        Unloaded += (_, _) => body.AbortAnimation("pulse");
    }
}

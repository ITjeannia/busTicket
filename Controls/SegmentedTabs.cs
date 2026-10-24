using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>
/// Sélecteur segmenté : « À venir | Passés », « Sam. 24 | Dim. 25 | Lun. 26 ».
/// L'indicateur bleu glisse vers l'onglet choisi.
/// </summary>
public class SegmentedTabs : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        Ui.Prop<SegmentedTabs, IList<string>>(nameof(Items), [], t => t.Rebuild());
    public static readonly BindableProperty SelectedIndexProperty =
        Ui.Prop<SegmentedTabs, int>(nameof(SelectedIndex), 0, t => t.OnSelectionChanged(), BindingMode.TwoWay);

    public IList<string> Items { get => (IList<string>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    private const double TrackHeight = 42;
    private const double Inset = 4;

    private readonly Grid _grid = new() { ColumnSpacing = 0 };
    private readonly Border _indicator = new()
    {
        StrokeThickness = 0,
        BackgroundColor = AppColors.Primary,
        StrokeShape = Ui.Rounded(AppSpacing.RadiusSm),
        HorizontalOptions = LayoutOptions.Start,
        HeightRequest = TrackHeight - 2 * Inset,
        Shadow = new Shadow { Brush = AppColors.Primary, Opacity = 0.25f, Radius = 6, Offset = new Point(0, 2) },
    };
    private readonly List<Label> _labels = [];

    public SegmentedTabs()
    {
        var track = new Grid { Padding = Inset, HeightRequest = TrackHeight };
        track.Add(_indicator);
        track.Add(_grid);
        track.SizeChanged += (_, _) => PlaceIndicator(animated: false);

        Content = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = AppColors.SurfaceMuted,
            StrokeShape = Ui.Rounded(AppSpacing.RadiusMd),
            Content = track,
        };
    }

    private double SegmentWidth => Items.Count == 0 ? 0 : (Width - 2 * Inset) / Items.Count;

    private void Rebuild()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _labels.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            var index = i;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var label = Ui.Text(Items[i], AppFonts.Caption, AppColors.TextSecondary, AppFonts.Medium);
            label.HorizontalOptions = LayoutOptions.Center;
            label.VerticalOptions = LayoutOptions.Center;
            _labels.Add(label);

            var cell = new ContentView { Content = label, BackgroundColor = Colors.Transparent };
            cell.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedIndex = index) });
            SemanticProperties.SetDescription(cell, Items[i]);
            _grid.Add(cell, i);
        }
        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        for (var i = 0; i < _labels.Count; i++)
            _labels[i].TextColor = i == SelectedIndex ? AppColors.TextOnPrimary : AppColors.TextSecondary;
        PlaceIndicator(animated: true);
    }

    private void PlaceIndicator(bool animated)
    {
        _indicator.IsVisible = SelectedIndex >= 0 && SelectedIndex < Items.Count;
        if (Width <= 0 || !_indicator.IsVisible) return;

        _indicator.WidthRequest = SegmentWidth;
        var x = SelectedIndex * SegmentWidth;
        if (animated) _ = _indicator.TranslateToAsync(x, 0, AppMotion.Normal, Easing.CubicInOut);
        else _indicator.TranslationX = x;
    }
}

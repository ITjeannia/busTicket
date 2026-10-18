using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Sélecteur en forme de pilule : « À venir | Passés », « Mer. 7 | Jeu. 8 | Ven. 9 ».</summary>
public class SegmentedTabs : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        Ui.Prop<SegmentedTabs, IList<string>>(nameof(Items), [], t => t.Rebuild());
    public static readonly BindableProperty SelectedIndexProperty =
        Ui.Prop<SegmentedTabs, int>(nameof(SelectedIndex), 0, t => t.Rebuild(), BindingMode.TwoWay);

    public IList<string> Items { get => (IList<string>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    private readonly Grid _grid = new() { ColumnSpacing = 0 };

    public SegmentedTabs()
    {
        Content = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = AppColors.SurfaceMuted,
            StrokeShape = Ui.Rounded(AppSpacing.RadiusPill),
            Padding = AppSpacing.Xs,
            Content = _grid,
        };
    }

    private void Rebuild()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            var index = i;
            var selected = i == SelectedIndex;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var label = Ui.Text(Items[i], AppFonts.Body, selected ? AppColors.TextOnPrimary : AppColors.TextSecondary, AppFonts.Medium);
            label.HorizontalOptions = LayoutOptions.Center;
            label.VerticalOptions = LayoutOptions.Center;

            var pill = new Border
            {
                StrokeThickness = 0,
                HeightRequest = 40,
                StrokeShape = Ui.Rounded(AppSpacing.RadiusPill),
                BackgroundColor = selected ? AppColors.Primary : Colors.Transparent,
                Content = label,
            };
            pill.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedIndex = index) });
            SemanticProperties.SetDescription(pill, Items[i]);
            _grid.Add(pill, i);
        }
    }
}

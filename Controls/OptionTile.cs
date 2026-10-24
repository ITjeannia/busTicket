using System.Windows.Input;
using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Choix exclusif sous forme de carte avec bouton radio (ex. moyen de paiement).</summary>
public class OptionTile : ContentView
{
    public static readonly BindableProperty TitleProperty = Ui.Prop<OptionTile, string>(nameof(Title), "", o => o.Refresh());
    public static readonly BindableProperty DescriptionProperty = Ui.Prop<OptionTile, string?>(nameof(Description), null, o => o.Refresh());
    public static readonly BindableProperty IconProperty = Ui.Prop<OptionTile, string>(nameof(Icon), "", o => o.Refresh());
    public static readonly BindableProperty AccentProperty = Ui.Prop<OptionTile, Color?>(nameof(Accent), null, o => o.Refresh());
    public static readonly BindableProperty IsSelectedProperty = Ui.Prop<OptionTile, bool>(nameof(IsSelected), false, o => o.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<OptionTile, ICommand?>(nameof(Command));
    public static readonly BindableProperty CommandParameterProperty = Ui.Prop<OptionTile, object?>(nameof(CommandParameter));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    /// <summary>Couleur de la pastille de l'icône (couleur de l'opérateur).</summary>
    public Color? Accent { get => (Color?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private readonly Label _icon = Ui.Icon("", 22);
    private readonly Border _iconTile = new() { WidthRequest = 42, HeightRequest = 42, StrokeThickness = 0, StrokeShape = Ui.Rounded(AppSpacing.RadiusSm) };
    private readonly Label _title = Ui.Text(size: AppFonts.Body, font: AppFonts.Medium);
    private readonly Label _description = Ui.Text(size: AppFonts.Small, color: AppColors.TextSecondary);
    private readonly Label _radio = Ui.Icon("", 24);
    private readonly Border _box = new() { StrokeShape = Ui.Rounded(AppSpacing.RadiusMd), Padding = new Thickness(14, 12) };

    public OptionTile()
    {
        _iconTile.Content = _icon;
        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 14,
        };
        row.Add(_iconTile, 0);
        row.Add(new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _title, _description } }, 1);
        row.Add(_radio, 2);
        _box.Content = row;
        _box.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (!IsEnabled || Command?.CanExecute(CommandParameter) != true) return;
                Command.Execute(CommandParameter);
                await Motion.PressAsync(_box);
            }),
        });
        Content = _box;
        Refresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Refresh();
    }

    private void Refresh()
    {
        var active = IsSelected && IsEnabled;
        var accent = Accent ?? AppColors.Primary;
        _box.BackgroundColor = !IsEnabled ? AppColors.SurfaceMuted : active ? AppColors.PrimarySoft : AppColors.Surface;
        _box.Stroke = active ? AppColors.Primary : AppColors.Divider;
        _box.StrokeThickness = active ? 2 : 1;
        _iconTile.BackgroundColor = IsEnabled ? accent : AppColors.Disabled;
        _icon.Text = Icon;
        _icon.TextColor = Colors.White;
        _title.Text = Title;
        _title.TextColor = IsEnabled ? AppColors.TextPrimary : AppColors.TextDisabled;
        _description.Text = Description;
        _description.IsVisible = Description is not null;
        _radio.Text = active ? MaterialIcons.RadioChecked : MaterialIcons.RadioUnchecked;
        _radio.TextColor = active ? AppColors.Primary : IsEnabled ? AppColors.Border : AppColors.Disabled;
        SemanticProperties.SetDescription(_box, $"{Title}{(IsSelected ? ", sélectionné" : "")}");
    }
}

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
    public static readonly BindableProperty IsSelectedProperty = Ui.Prop<OptionTile, bool>(nameof(IsSelected), false, o => o.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<OptionTile, ICommand?>(nameof(Command));
    public static readonly BindableProperty CommandParameterProperty = Ui.Prop<OptionTile, object?>(nameof(CommandParameter));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private readonly Label _icon = Ui.Icon("", 24);
    private readonly Label _title = Ui.Text(size: AppFonts.Subtitle, font: AppFonts.Medium);
    private readonly Label _description = Ui.Text(size: AppFonts.Caption, color: AppColors.TextSecondary);
    private readonly Label _radio = Ui.Icon("", 24);
    private readonly Border _box = new() { StrokeShape = Ui.Rounded(AppSpacing.RadiusMd), Padding = AppSpacing.Md };

    public OptionTile()
    {
        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 20,
        };
        row.Add(_icon, 0);
        row.Add(new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _title, _description } }, 1);
        row.Add(_radio, 2);
        _box.Content = row;
        _box.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                if (IsEnabled && Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);
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
        _box.BackgroundColor = !IsEnabled ? AppColors.SurfaceMuted : active ? AppColors.PrimarySoft : AppColors.Surface;
        _box.Stroke = active ? AppColors.Primary : AppColors.Border;
        _box.StrokeThickness = active ? 2 : 1;
        _icon.Text = Icon;
        _icon.TextColor = active ? AppColors.Primary : IsEnabled ? AppColors.TextPrimary : AppColors.TextDisabled;
        _title.Text = Title;
        _title.TextColor = IsEnabled ? AppColors.TextPrimary : AppColors.TextDisabled;
        _description.Text = Description;
        _description.IsVisible = Description is not null;
        _description.TextColor = IsEnabled ? AppColors.TextSecondary : AppColors.TextDisabled;
        _radio.Text = active ? MaterialIcons.RadioChecked : MaterialIcons.RadioUnchecked;
        _radio.TextColor = active ? AppColors.Primary : IsEnabled ? AppColors.TextSecondary : AppColors.Disabled;
        SemanticProperties.SetDescription(_box, $"{Title}{(IsSelected ? ", sélectionné" : "")}");
    }
}

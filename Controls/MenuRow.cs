using System.Windows.Input;
using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Ligne de menu : pastille d'icône + texte (+ sous-titre) + chevron. IsDestructive = rouge, sans chevron.</summary>
public class MenuRow : ContentView
{
    public static readonly BindableProperty IconProperty = Ui.Prop<MenuRow, string>(nameof(Icon), "", r => r.Refresh());
    public static readonly BindableProperty TextProperty = Ui.Prop<MenuRow, string>(nameof(Text), "", r => r.Refresh());
    public static readonly BindableProperty SubtitleProperty = Ui.Prop<MenuRow, string?>(nameof(Subtitle), null, r => r.Refresh());
    public static readonly BindableProperty IsDestructiveProperty = Ui.Prop<MenuRow, bool>(nameof(IsDestructive), false, r => r.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<MenuRow, ICommand?>(nameof(Command));
    public static readonly BindableProperty CommandParameterProperty = Ui.Prop<MenuRow, object?>(nameof(CommandParameter));

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public bool IsDestructive { get => (bool)GetValue(IsDestructiveProperty); set => SetValue(IsDestructiveProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private readonly Label _icon = Ui.Icon("", 20);
    private readonly Border _tile = new() { WidthRequest = 36, HeightRequest = 36, StrokeThickness = 0, StrokeShape = Ui.Rounded(AppSpacing.RadiusSm) };
    private readonly Label _text = Ui.Text(font: AppFonts.Medium, size: AppFonts.Body);
    private readonly Label _subtitle = Ui.Text(size: AppFonts.Small, color: AppColors.TextSecondary);
    private readonly Label _chevron = Ui.Icon(MaterialIcons.ChevronRight, 22, AppColors.Disabled);

    public MenuRow()
    {
        _tile.Content = _icon;
        _subtitle.LineBreakMode = LineBreakMode.TailTruncation;
        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 14,
            Padding = new Thickness(AppSpacing.Md, 10),
            MinimumHeightRequest = 60,
            BackgroundColor = Colors.Transparent,
        };
        row.Add(_tile, 0);
        row.Add(new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center, Children = { _text, _subtitle } }, 1);
        row.Add(_chevron, 2);
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                await row.FadeToAsync(0.55, 60);
                await row.FadeToAsync(1, 120);
                if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);
            }),
        });
        Content = row;
        Refresh();
    }

    private void Refresh()
    {
        var color = IsDestructive ? AppColors.Error : AppColors.Primary;
        _tile.BackgroundColor = IsDestructive ? AppColors.ErrorSoft : AppColors.PrimarySoft;
        _icon.Text = Icon;
        _icon.TextColor = color;
        _text.Text = Text;
        _text.TextColor = IsDestructive ? AppColors.Error : AppColors.TextPrimary;
        _subtitle.Text = Subtitle;
        _subtitle.IsVisible = !string.IsNullOrEmpty(Subtitle);
        _chevron.IsVisible = !IsDestructive;
        SemanticProperties.SetDescription(this, Text);
    }
}

using System.Windows.Input;
using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Ligne de menu : icône + texte + chevron. IsDestructive = rouge, sans chevron (déconnexion).</summary>
public class MenuRow : ContentView
{
    public static readonly BindableProperty IconProperty = Ui.Prop<MenuRow, string>(nameof(Icon), "", r => r.Refresh());
    public static readonly BindableProperty TextProperty = Ui.Prop<MenuRow, string>(nameof(Text), "", r => r.Refresh());
    public static readonly BindableProperty IsDestructiveProperty = Ui.Prop<MenuRow, bool>(nameof(IsDestructive), false, r => r.Refresh());
    public static readonly BindableProperty CommandProperty = Ui.Prop<MenuRow, ICommand?>(nameof(Command));
    public static readonly BindableProperty CommandParameterProperty = Ui.Prop<MenuRow, object?>(nameof(CommandParameter));

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsDestructive { get => (bool)GetValue(IsDestructiveProperty); set => SetValue(IsDestructiveProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private readonly Label _icon = Ui.Icon("", 24);
    private readonly Label _text = Ui.Text();
    private readonly Label _chevron = Ui.Icon(MaterialIcons.ChevronRight, 24);

    public MenuRow()
    {
        _text.VerticalOptions = LayoutOptions.Center;
        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 20,
            Padding = new Thickness(AppSpacing.Md, 0),
            MinimumHeightRequest = 56,
        };
        row.Add(_icon, 0);
        row.Add(_text, 1);
        row.Add(_chevron, 2);
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter); }),
        });
        Content = row;
        Refresh();
    }

    private void Refresh()
    {
        var color = IsDestructive ? AppColors.Error : AppColors.TextPrimary;
        _icon.Text = Icon;
        _icon.TextColor = color;
        _text.Text = Text;
        _text.TextColor = color;
        _chevron.IsVisible = !IsDestructive;
        SemanticProperties.SetDescription(this, Text);
    }
}

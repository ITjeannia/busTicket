using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Ligne « libellé ........ valeur » (ex. détail du prix). IsTotal met la valeur en avant.</summary>
public class InfoRow : ContentView
{
    public static readonly BindableProperty LabelProperty = Ui.Prop<InfoRow, string>(nameof(Label), "", r => r.Refresh());
    public static readonly BindableProperty ValueProperty = Ui.Prop<InfoRow, string>(nameof(Value), "", r => r.Refresh());
    public static readonly BindableProperty IsTotalProperty = Ui.Prop<InfoRow, bool>(nameof(IsTotal), false, r => r.Refresh());

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool IsTotal { get => (bool)GetValue(IsTotalProperty); set => SetValue(IsTotalProperty, value); }

    private readonly Label _label = Ui.Text(color: AppColors.TextSecondary);
    private readonly Label _value = Ui.Text(font: AppFonts.Medium);

    public InfoRow()
    {
        _label.VerticalOptions = _value.VerticalOptions = LayoutOptions.Center;
        var row = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], Padding = new Thickness(0, 6) };
        row.Add(_label, 0);
        row.Add(_value, 1);
        Content = row;
    }

    private void Refresh()
    {
        _label.Text = Label;
        _value.Text = Value;
        _value.FontSize = IsTotal ? AppFonts.Title : AppFonts.Body;
        _value.FontFamily = IsTotal ? AppFonts.Bold : AppFonts.Medium;
        _value.TextColor = IsTotal ? AppColors.Primary : AppColors.TextPrimary;
    }
}

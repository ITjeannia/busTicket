using BusTicket.Helpers;
using BusTicket.Theme;

namespace BusTicket.Controls;

/// <summary>Champ de date au même style que <see cref="AppSelectField"/> (sélecteur natif du téléphone).</summary>
public class AppDateField : ContentView
{
    public static readonly BindableProperty LabelProperty = Ui.Prop<AppDateField, string>(nameof(Label), "Date", f => f._label.Text = f.Label);
    public static readonly BindableProperty DateProperty = Ui.Prop<AppDateField, DateTime>(nameof(Date), DateTime.Today, mode: BindingMode.TwoWay);

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public DateTime Date { get => (DateTime)GetValue(DateProperty); set => SetValue(DateProperty, value); }

    private readonly Label _label = Ui.Text("Date", AppFonts.Caption, font: AppFonts.Medium);

    public AppDateField()
    {
        var picker = new DatePicker
        {
            // Le sélecteur Windows ajoute sa propre icône : format court pour tenir dans le champ.
            Format = DeviceInfo.Platform == DevicePlatform.WinUI ? "dd/MM" : "ddd d MMM",
            FontFamily = AppFonts.Regular,
            FontSize = AppFonts.Body,
            TextColor = AppColors.TextPrimary,
            MinimumDate = DateTime.Today,
            MaximumDate = DateTime.Today.AddDays(90),
            VerticalOptions = LayoutOptions.Center,
        };
        picker.SetBinding(DatePicker.DateProperty, new Binding(nameof(Date), BindingMode.TwoWay, source: this));

        var row = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)], ColumnSpacing = 12 };
        // Sous Windows, le sélecteur affiche déjà une icône de calendrier : pas de doublon.
        if (DeviceInfo.Platform != DevicePlatform.WinUI) row.Add(Ui.Icon(MaterialIcons.Calendar), 0);
        row.Add(picker, 1);

        var box = new Border
        {
            StrokeShape = Ui.Rounded(AppSpacing.RadiusSm),
            Stroke = AppColors.Border,
            BackgroundColor = AppColors.Surface,
            HeightRequest = AppSpacing.ControlHeight,
            Padding = new Thickness(12, 0),
            Content = row,
        };

        Content = new VerticalStackLayout { Spacing = 6, Children = { _label, box } };
    }
}

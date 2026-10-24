using System.ComponentModel;
using System.Windows.Input;
using BusTicket.Controls;
using BusTicket.Helpers;
using BusTicket.Theme;
using BusTicket.ViewModels.Items;

namespace BusTicket.Views.Booking;

/// <summary>Un siège dans le plan du bus. Libre = blanc, choisi = bleu (petit rebond), occupé = gris avec une croix.</summary>
public class SeatView : ContentView
{
    public const double Size = 42;

    public static readonly BindableProperty SeatProperty = BindableProperty.Create(
        nameof(Seat), typeof(SeatItem), typeof(SeatView), propertyChanged: (b, o, n) => ((SeatView)b).OnSeatChanged((SeatItem?)o, (SeatItem?)n));
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SeatView));

    public SeatItem? Seat { get => (SeatItem?)GetValue(SeatProperty); set => SetValue(SeatProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private readonly Label _label = new() { FontFamily = AppFonts.Medium, FontSize = 14, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly BoxView _headrest = new() { HeightRequest = 4, CornerRadius = 2, Margin = new Thickness(8, 4, 8, 0), VerticalOptions = LayoutOptions.Start };
    private readonly Border _box = new() { WidthRequest = Size, HeightRequest = Size, StrokeShape = Ui.Rounded(AppSpacing.RadiusSm), StrokeThickness = 1.5 };
    private SeatStatus? _lastStatus;

    public SeatView()
    {
        var content = new Grid();
        content.Add(_headrest);
        content.Add(_label);
        _box.Content = content;
        _box.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { if (Seat is not null) Command?.Execute(Seat); }),
        });
        Content = _box;
    }

    private void OnSeatChanged(SeatItem? oldSeat, SeatItem? newSeat)
    {
        if (oldSeat is not null) oldSeat.PropertyChanged -= OnSeatPropertyChanged;
        if (newSeat is not null) newSeat.PropertyChanged += OnSeatPropertyChanged;
        _lastStatus = null;
        Refresh();
    }

    private void OnSeatPropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private async void Refresh()
    {
        IsVisible = Seat is not null;
        if (Seat is null) return;

        var status = Seat.Status;
        var (background, border, text, headrest) = status switch
        {
            SeatStatus.Selected => (AppColors.Primary, AppColors.PrimaryDark, AppColors.TextOnPrimary, AppColors.PrimaryMuted),
            SeatStatus.Booked => (AppColors.SurfaceMuted, AppColors.Divider, AppColors.TextDisabled, AppColors.Divider),
            _ => (AppColors.Surface, AppColors.Border, AppColors.TextPrimary, AppColors.PrimarySoft),
        };
        _box.BackgroundColor = background;
        _box.Stroke = border;
        _headrest.Color = headrest;

        var booked = status == SeatStatus.Booked;
        _label.FontFamily = booked ? AppFonts.Icons : AppFonts.Medium;
        _label.FontSize = booked ? 16 : 14;
        _label.Text = booked ? MaterialIcons.Close : Seat.Number.ToString();
        _label.TextColor = text;
        SemanticProperties.SetDescription(_box, $"Siège {Seat.Number}{(booked ? ", occupé" : status == SeatStatus.Selected ? ", choisi" : "")}");

        // Petit rebond quand le siège vient d'être choisi.
        var justSelected = _lastStatus is not null && _lastStatus != SeatStatus.Selected && status == SeatStatus.Selected;
        _lastStatus = status;
        if (justSelected)
        {
            await _box.ScaleToAsync(1.15, 90, Easing.CubicOut);
            await _box.ScaleToAsync(1, 160, Easing.SpringOut);
        }
    }
}

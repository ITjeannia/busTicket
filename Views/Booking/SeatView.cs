using System.ComponentModel;
using System.Windows.Input;
using BusTicket.Controls;
using BusTicket.Helpers;
using BusTicket.Theme;
using BusTicket.ViewModels.Items;

namespace BusTicket.Views.Booking;

/// <summary>Un siège dans le plan du bus. Libre = blanc, choisi = bleu, occupé = gris avec une croix.</summary>
public class SeatView : ContentView
{
    public const double Size = 44;

    public static readonly BindableProperty SeatProperty = BindableProperty.Create(
        nameof(Seat), typeof(SeatItem), typeof(SeatView), propertyChanged: (b, o, n) => ((SeatView)b).OnSeatChanged((SeatItem?)o, (SeatItem?)n));
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SeatView));

    public SeatItem? Seat { get => (SeatItem?)GetValue(SeatProperty); set => SetValue(SeatProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private readonly Label _label = new() { FontFamily = AppFonts.Medium, FontSize = 15, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly Border _box = new() { WidthRequest = Size, HeightRequest = Size, StrokeShape = Ui.Rounded(AppSpacing.RadiusSm) };

    public SeatView()
    {
        _box.Content = _label;
        _box.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { if (Seat is not null) Command?.Execute(Seat); }),
        });
        Content = _box;
    }

    /// <summary>Couleurs (fond, bordure) selon l'état — partagées avec la légende.</summary>
    public static (Color Background, Color Border) ColorsOf(SeatStatus status) => status switch
    {
        SeatStatus.Selected => (AppColors.Primary, AppColors.Primary),
        SeatStatus.Booked => (AppColors.Disabled, AppColors.Disabled),
        _ => (AppColors.Surface, AppColors.Border),
    };

    private void OnSeatChanged(SeatItem? oldSeat, SeatItem? newSeat)
    {
        if (oldSeat is not null) oldSeat.PropertyChanged -= OnSeatPropertyChanged;
        if (newSeat is not null) newSeat.PropertyChanged += OnSeatPropertyChanged;
        Refresh();
    }

    private void OnSeatPropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        IsVisible = Seat is not null;
        if (Seat is null) return;

        var (background, border) = ColorsOf(Seat.Status);
        _box.BackgroundColor = background;
        _box.Stroke = border;

        var booked = Seat.Status == SeatStatus.Booked;
        _label.FontFamily = booked ? AppFonts.Icons : AppFonts.Medium;
        _label.FontSize = booked ? 18 : 15;
        _label.Text = booked ? MaterialIcons.Close : Seat.Number.ToString();
        _label.TextColor = Seat.Status switch
        {
            SeatStatus.Selected => AppColors.TextOnPrimary,
            SeatStatus.Booked => AppColors.TextSecondary,
            _ => AppColors.TextPrimary,
        };
        SemanticProperties.SetDescription(_box, $"Siège {Seat.Number}{(booked ? ", occupé" : "")}");
    }
}

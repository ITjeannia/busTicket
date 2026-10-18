using System.Collections.ObjectModel;
using BusTicket.Helpers;
using BusTicket.Models;
using BusTicket.Services;
using BusTicket.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Maquette 05 — un siège par passager. Plan : 2 sièges + allée + 2 sièges.</summary>
public partial class SeatSelectionViewModel(Navigator navigator) : ObservableObject, IQueryAttributable
{
    private const int SeatsPerRow = 4;

    private Trip? _trip;
    private int _passengers = 1;
    private readonly List<SeatItem> _seats = [];

    public ObservableCollection<SeatRow> Rows { get; } = [];

    [ObservableProperty] public partial string TripInfo { get; set; } = "";
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string PriceText { get; set; } = "";
    [ObservableProperty] public partial bool CanContinue { get; set; }

    private List<int> SelectedNumbers =>
        _seats.Where(s => s.Status == SeatStatus.Selected).Select(s => s.Number).Order().ToList();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _trip = (Trip)query["trip"];
        _passengers = (int)query["passengers"];
        TripInfo = $"{Formatters.ShortDate(_trip.Departure)} · {Formatters.Time(_trip.Departure)} → " +
                   $"{Formatters.Time(_trip.Arrival)} · {_trip.Company}";
        PriceText = Formatters.Price(_trip.Price * _passengers);

        _seats.Clear();
        for (var number = 1; number <= _trip.TotalSeats; number++)
        {
            var status = _trip.BookedSeats.Contains(number) ? SeatStatus.Booked : SeatStatus.Available;
            _seats.Add(new SeatItem(number, status));
        }

        Rows.Clear();
        SeatItem? At(int i) => i < _seats.Count ? _seats[i] : null;
        for (var i = 0; i < _seats.Count; i += SeatsPerRow)
            Rows.Add(new SeatRow(At(i), At(i + 1), At(i + 2), At(i + 3)));

        UpdateSummary();
    }

    [RelayCommand]
    private async Task ToggleSeatAsync(SeatItem seat)
    {
        switch (seat.Status)
        {
            case SeatStatus.Booked:
                return;
            case SeatStatus.Selected:
                seat.Status = SeatStatus.Available;
                break;
            default:
                // Avec un seul passager, toucher un autre siège remplace le choix.
                if (_passengers == 1)
                    foreach (var s in _seats.Where(s => s.Status == SeatStatus.Selected)) s.Status = SeatStatus.Available;

                if (SelectedNumbers.Count == _passengers)
                {
                    await navigator.InfoAsync($"Vous avez déjà choisi {_passengers} sièges. Touchez un siège pour le libérer.");
                    return;
                }
                seat.Status = SeatStatus.Selected;
                break;
        }
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var selected = SelectedNumbers;
        Summary = selected.Count == 0
            ? $"Choisissez {(_passengers > 1 ? $"{_passengers} sièges" : "un siège")}"
            : $"{Formatters.Seats(selected)} · {Formatters.Passengers(_passengers)}";
        CanContinue = selected.Count == _passengers;
    }

    [RelayCommand]
    private Task ContinueAsync() =>
        navigator.GoToAsync(Routes.Payment, new Dictionary<string, object>
        {
            ["trip"] = _trip!,
            ["seats"] = SelectedNumbers,
        });
}

using System.Collections.ObjectModel;
using BusTicket.Core;
using BusTicket.Services;
using BusTicket.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Choix des sièges : un par passager. Plan 2 + 2 (Classique) ou 2 + 1 (VIP).</summary>
public partial class SeatSelectionViewModel(Backend backend, Navigator navigator) : ObservableObject, IQueryAttributable
{
    private TripDto? _trip;
    private int _passengers = 1;
    private readonly List<SeatItem> _seats = [];

    public ObservableCollection<SeatRow> Rows { get; } = [];

    [ObservableProperty] public partial string Route { get; set; } = "";
    [ObservableProperty] public partial string TripInfo { get; set; } = "";
    [ObservableProperty] public partial string ClassText { get; set; } = "";
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string PriceText { get; set; } = "";
    [ObservableProperty] public partial string AvailabilityText { get; set; } = "";
    [ObservableProperty] public partial bool CanContinue { get; set; }
    [ObservableProperty] public partial bool IsVip { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }

    private List<int> SelectedNumbers =>
        _seats.Where(s => s.Status == SeatStatus.Selected).Select(s => s.Number).Order().ToList();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _trip = (TripDto)query["trip"];
        _passengers = (int)query["passengers"];
        Route = _trip.Route;
        TripInfo = $"{Formatters.ShortDate(_trip.Departure)} · {Formatters.Time(_trip.Departure)} → {Formatters.Time(_trip.Arrival)} · {_trip.Company}";
        ClassText = _trip.ServiceClass;
        IsVip = _trip.IsVip;
        BuildPlan(_trip);
        _ = RefreshSeatsAsync();
    }

    /// <summary>Les places ont pu partir depuis l'affichage des résultats : on recharge le plan.</summary>
    private async Task RefreshSeatsAsync()
    {
        if (_trip is null) return;
        IsLoading = true;
        try
        {
            var fresh = await backend.GetTripAsync(_trip.Id);
            var selected = SelectedNumbers;
            _trip = fresh;
            foreach (var seat in _seats)
            {
                var booked = fresh.BookedSeats.Contains(seat.Number);
                seat.Status = booked ? SeatStatus.Booked : selected.Contains(seat.Number) ? SeatStatus.Selected : SeatStatus.Available;
            }
            UpdateSummary();
        }
        catch (BusTicketException)
        {
            // Départ introuvable : on garde le plan reçu de la liste.
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildPlan(TripDto trip)
    {
        _seats.Clear();
        for (var number = 1; number <= trip.TotalSeats; number++)
            _seats.Add(new SeatItem(number, trip.BookedSeats.Contains(number) ? SeatStatus.Booked : SeatStatus.Available));

        Rows.Clear();
        SeatItem? At(int i) => i < _seats.Count ? _seats[i] : null;
        for (var i = 0; i < _seats.Count; i += trip.SeatsPerRow)
        {
            Rows.Add(trip.SeatsPerRow == 3
                ? new SeatRow(At(i), At(i + 1), null, At(i + 2))
                : new SeatRow(At(i), At(i + 1), At(i + 2), At(i + 3)));
        }
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
                    await navigator.InfoAsync($"Vous avez déjà choisi {_passengers} sièges. Touchez un siège choisi pour le libérer.");
                    return;
                }
                seat.Status = SeatStatus.Selected;
                break;
        }
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_trip is null) return;
        var selected = SelectedNumbers;
        var remaining = _passengers - selected.Count;
        Summary = selected.Count == 0
            ? $"Choisissez {(_passengers > 1 ? $"{_passengers} sièges" : "un siège")}"
            : remaining > 0
                ? $"{Formatters.Seats(selected)} · encore {remaining} à choisir"
                : $"{Formatters.Seats(selected)} · {Formatters.Passengers(_passengers)}";
        PriceText = Formatters.Price(_trip.Price * _passengers);
        AvailabilityText = $"{_trip.AvailableSeats} places libres sur {_trip.TotalSeats}";
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

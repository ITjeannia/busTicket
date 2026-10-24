using BusTicket.Core;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Écran « Paiement réussi » : reçu, puis accès au billet et au PDF.</summary>
public partial class ReceiptViewModel(TicketFiles files, Navigator navigator) : ObservableObject, IQueryAttributable
{
    private BookingDto? _booking;

    [ObservableProperty] public partial string Reference { get; set; } = "";
    [ObservableProperty] public partial string Route { get; set; } = "";
    [ObservableProperty] public partial string TripLine { get; set; } = "";
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    [ObservableProperty] public partial string TransactionId { get; set; } = "";
    [ObservableProperty] public partial string PaidAt { get; set; } = "";
    [ObservableProperty] public partial string MethodText { get; set; } = "";
    [ObservableProperty] public partial string AccountText { get; set; } = "";
    [ObservableProperty] public partial string TicketsLabel { get; set; } = "";
    [ObservableProperty] public partial string TicketsAmount { get; set; } = "";
    [ObservableProperty] public partial string FeeText { get; set; } = "";
    [ObservableProperty] public partial string Passenger { get; set; } = "";
    [ObservableProperty] public partial bool IsDownloading { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _booking = (BookingDto)query["booking"];
        var trip = _booking.Trip;
        var pay = _booking.Payment;
        Reference = _booking.Reference;
        Route = trip.Route;
        TripLine = $"{Formatters.ShortDate(trip.Departure)} · {Formatters.Time(trip.Departure)} · {Formatters.Seats(_booking.Seats)}";
        TotalText = Formatters.Price(pay.Total);
        TransactionId = pay.TransactionId;
        PaidAt = Formatters.Timestamp(pay.PaidAt);
        MethodText = pay.MethodLabel;
        AccountText = pay.AccountMasked;
        TicketsLabel = $"Billets ({_booking.Seats.Count} × {Formatters.Price(trip.Price)})";
        TicketsAmount = Formatters.Price(pay.TicketsAmount);
        FeeText = Formatters.Price(pay.ServiceFee);
        Passenger = _booking.PassengerName;
    }

    [RelayCommand]
    private Task ViewTicketAsync() =>
        navigator.ShowTicketAsync(new Dictionary<string, object> { ["booking"] = _booking! });

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (_booking is null) return;
        IsDownloading = true;
        try
        {
            var saved = await files.DownloadAsync(_booking);
            if (saved is not null &&
                await navigator.ConfirmAsync("Billet téléchargé", $"Le billet PDF est enregistré :\n{saved}", "Ouvrir", "OK"))
                await files.OpenAsync(_booking);
        }
        catch (Exception e)
        {
            await navigator.InfoAsync($"Le téléchargement a échoué : {e.Message}");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private Task GoHomeAsync() => navigator.GoToAsync(Routes.Search);
}

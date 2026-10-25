using BusTicket.Core;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;

namespace BusTicket.ViewModels;

/// <summary>Billet : QR code à présenter, détails, téléchargement PDF et partage.</summary>
public partial class TicketViewModel(TicketFiles files, Navigator navigator) : ObservableObject, IQueryAttributable
{
    private BookingDto? _booking;

    [ObservableProperty] public partial string From { get; set; } = "";
    [ObservableProperty] public partial string To { get; set; } = "";
    [ObservableProperty] public partial string DepartureTime { get; set; } = "";
    [ObservableProperty] public partial string ArrivalTime { get; set; } = "";
    [ObservableProperty] public partial string DurationText { get; set; } = "";
    [ObservableProperty] public partial string DateLine { get; set; } = "";
    [ObservableProperty] public partial string Passenger { get; set; } = "";
    [ObservableProperty] public partial string Company { get; set; } = "";
    [ObservableProperty] public partial string ServiceClass { get; set; } = "";
    [ObservableProperty] public partial string SeatLabel { get; set; } = "Siège";
    [ObservableProperty] public partial string SeatValue { get; set; } = "";
    [ObservableProperty] public partial string Reference { get; set; } = "";
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    [ObservableProperty] public partial string PaymentText { get; set; } = "";
    [ObservableProperty] public partial string BoardingText { get; set; } = "";
    [ObservableProperty] public partial string CodeCaption { get; set; } = "";
    [ObservableProperty] public partial string AgencyName { get; set; } = "";
    [ObservableProperty] public partial string AgencyPlace { get; set; } = "";
    [ObservableProperty] public partial string? ExpiredMessage { get; set; }
    [ObservableProperty] public partial string? BoardedMessage { get; set; }
    [ObservableProperty] public partial ImageSource? QrCode { get; set; }
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial bool ArrivesNextDay { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsValid))]
    public partial bool IsExpired { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsValid))]
    public partial bool IsBoarded { get; set; }

    public bool IsValid => !IsExpired && !IsBoarded;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var booking = (BookingDto)query["booking"];
        _booking = booking;
        var trip = booking.Trip;

        From = trip.From;
        To = trip.To;
        DepartureTime = Formatters.Time(trip.Departure);
        ArrivalTime = Formatters.Time(trip.Arrival);
        ArrivesNextDay = trip.ArrivesNextDay;
        DurationText = $"{Formatters.Duration(trip.Duration)} · {trip.StopsLabel}";
        DateLine = Formatters.LongDate(trip.Departure);
        Passenger = booking.PassengerName;
        Company = trip.Company;
        ServiceClass = trip.ServiceClass;
        SeatLabel = booking.Seats.Count > 1 ? "Sièges" : "Siège";
        SeatValue = string.Join(", ", booking.Seats);
        Reference = booking.Reference;
        TotalText = Formatters.Price(booking.Payment.Total);
        PaymentText = $"{booking.Payment.MethodLabel} · {booking.Payment.AccountMasked}";
        BoardingText = $"Présentez-vous avant {Formatters.Time(trip.Departure.AddMinutes(-30))}";
        var agency = Catalog.AgencyFor(trip.Company, trip.From);
        AgencyName = agency.Name;
        AgencyPlace = agency.Place;
        IsExpired = booking.IsExpired;
        IsBoarded = booking.IsBoarded;

        CodeCaption = IsExpired ? "Code désactivé"
            : IsBoarded ? "Billet déjà contrôlé"
            : "Présentez ce code au contrôleur";
        ExpiredMessage = IsExpired
            ? $"Ce billet a expiré le {Formatters.DayMonth(trip.Arrival)} à {Formatters.Time(trip.Arrival)}. Il ne peut plus être présenté au contrôle."
            : null;
        BoardedMessage = !IsExpired && IsBoarded
            ? $"Embarquement validé{(booking.BoardedAt is { } at ? $" le {Formatters.Timestamp(at)}" : "")}. Bon voyage !"
            : null;
        QrCode = CreateQrCode(booking.QrPayload, !IsValid);
    }

    /// <summary>QR code noir (valide) ou gris (expiré, déjà utilisé), généré à partir du contenu signé.</summary>
    private static ImageSource CreateQrCode(string data, bool disabled)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        byte[] dark = disabled ? [0xBD, 0xBD, 0xBD, 0xFF] : [0x1C, 0x1B, 0x1F, 0xFF];
        byte[] light = [0xFF, 0xFF, 0xFF, 0xFF];
        var png = new PngByteQRCode(qrData).GetGraphic(12, dark, light, drawQuietZones: false);
        return ImageSource.FromStream(() => new MemoryStream(png));
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (_booking is null) return;
        IsBusy = true;
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
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (_booking is null) return;
        try { await files.ShareAsync(_booking); }
        catch (Exception e) { await navigator.InfoAsync($"Partage impossible : {e.Message}"); }
    }

    [RelayCommand]
    private async Task OpenPdfAsync()
    {
        if (_booking is null) return;
        try { await files.OpenAsync(_booking); }
        catch (Exception e) { await navigator.InfoAsync($"Aucune application ne peut ouvrir le PDF : {e.Message}"); }
    }

    /// <summary>Itinéraire Google Maps vers l'agence de départ.</summary>
    [RelayCommand]
    private async Task OpenDirectionsAsync()
    {
        if (_booking is null) return;
        var agency = Catalog.AgencyFor(_booking.Trip.Company, _booking.Trip.From);
        try { await Launcher.Default.OpenAsync(new Uri(agency.DirectionsUrl)); }
        catch (Exception e) { await navigator.InfoAsync($"Impossible d'ouvrir la carte : {e.Message}"); }
    }

    [RelayCommand]
    private async Task CopyReferenceAsync()
    {
        await Clipboard.Default.SetTextAsync(Reference);
        await navigator.InfoAsync($"Référence {Reference} copiée.");
    }
}

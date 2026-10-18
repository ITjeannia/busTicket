using BusTicket.Helpers;
using BusTicket.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using QRCoder;

namespace BusTicket.ViewModels;

/// <summary>Maquettes 08 (billet valide avec QR code) et 08b (billet expiré).</summary>
public partial class TicketViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] public partial string Route { get; set; } = "";
    [ObservableProperty] public partial string DateLine { get; set; } = "";
    [ObservableProperty] public partial string Passenger { get; set; } = "";
    [ObservableProperty] public partial string Company { get; set; } = "";
    [ObservableProperty] public partial string SeatLabel { get; set; } = "Siège";
    [ObservableProperty] public partial string SeatValue { get; set; } = "";
    [ObservableProperty] public partial string Reference { get; set; } = "";
    [ObservableProperty] public partial string CodeCaption { get; set; } = "";
    [ObservableProperty] public partial string? ExpiredMessage { get; set; }
    [ObservableProperty] public partial ImageSource? QrCode { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsValid))]
    public partial bool IsExpired { get; set; }

    public bool IsValid => !IsExpired;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var booking = (Booking)query["booking"];
        var trip = booking.Trip;

        Route = trip.Route;
        DateLine = $"{Formatters.LongDate(trip.Departure)} · {Formatters.Time(trip.Departure)} – {Formatters.Time(trip.Arrival)}";
        Passenger = booking.PassengerName;
        Company = trip.Company;
        SeatLabel = booking.Seats.Count > 1 ? "Sièges" : "Siège";
        SeatValue = string.Join(", ", booking.Seats);
        Reference = booking.Reference;
        IsExpired = booking.IsExpired;
        CodeCaption = IsExpired ? "Code désactivé" : "Présentez ce code au contrôleur";
        ExpiredMessage = IsExpired
            ? $"Ce billet a expiré le {Formatters.DayMonth(trip.Arrival)} à {Formatters.Time(trip.Arrival)}. Il ne peut plus être présenté au contrôle."
            : null;
        QrCode = CreateQrCode(booking.Reference, IsExpired);
    }

    /// <summary>QR code noir (valide) ou gris (expiré) généré à partir de la référence.</summary>
    private static ImageSource CreateQrCode(string data, bool disabled)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        byte[] dark = disabled ? [0xBD, 0xBD, 0xBD, 0xFF] : [0x1C, 0x1B, 0x1F, 0xFF];
        byte[] light = [0xFF, 0xFF, 0xFF, 0xFF];
        var png = new PngByteQRCode(qrData).GetGraphic(12, dark, light, drawQuietZones: false);
        return ImageSource.FromStream(() => new MemoryStream(png));
    }
}

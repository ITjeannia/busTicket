using BusTicket.Core;
using BusTicket.Helpers;

namespace BusTicket.ViewModels.Items;

/// <summary>Un départ tel qu'affiché dans la liste des résultats.</summary>
public record TripItem(TripDto Trip, int Passengers, int Index)
{
    public string DepartureTime => Formatters.Time(Trip.Departure);
    public string ArrivalTime => Formatters.Time(Trip.Arrival);
    public string From => Trip.From;
    public string To => Trip.To;
    public string DurationText => Formatters.Duration(Trip.Duration);
    public string StopsText => Trip.StopsLabel;
    public string PriceText => Trip.IsFull ? "Complet" : Formatters.Price(Trip.Price);
    public string Company => Trip.Company;
    public string ServiceClass => Trip.ServiceClass;
    public bool IsVip => Trip.IsVip;
    public bool IsClassic => !Trip.IsVip;

    /// <summary>« Arrivée le lendemain » pour les départs de nuit.</summary>
    public bool ArrivesNextDay => Trip.Arrival.Date > Trip.Departure.Date;

    public bool IsFull => Trip.IsFull;
    public bool IsAvailable => !Trip.IsFull;
    public bool HasEnoughSeats => Trip.AvailableSeats >= Passengers;

    /// <summary>Moins de 6 places : on prévient.</summary>
    public bool IsAlmostFull => IsAvailable && Trip.AvailableSeats <= 5;

    public string SeatsText => Trip.AvailableSeats switch
    {
        <= 0 => "Complet",
        1 => "Dernière place",
        <= 5 => $"Plus que {Trip.AvailableSeats} places",
        _ => $"{Trip.AvailableSeats} places libres",
    };

    /// <summary>Icônes des équipements (climatisation, Wi-Fi…).</summary>
    public IReadOnlyList<string> AmenityIcons => Trip.Amenities.Select(a => a switch
    {
        "Wi-Fi" => MaterialIcons.Wifi,
        "Prises USB" => MaterialIcons.Usb,
        "Collation" => MaterialIcons.Cafe,
        "Climatisation" => MaterialIcons.AcUnit,
        _ => MaterialIcons.Luggage,
    }).ToList();

    /// <summary>Décalage d'apparition (ms) pour l'animation en cascade.</summary>
    public int EntranceDelay => Math.Min(Index, 6) * 60;
}

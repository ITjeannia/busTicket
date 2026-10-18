using BusTicket.Models;

namespace BusTicket.Services;

/// <summary>
/// Données fictives utilisées tant qu'il n'y a pas de serveur (API).
/// Pour brancher une vraie API, modifier les services de ce dossier sans toucher aux écrans.
/// </summary>
public static class MockData
{
    /// <summary>Mettre à true pour voir l'écran « Connexion impossible ».</summary>
    public static bool SimulateNetworkError { get; set; }

    public const int ServiceFee = 200;

    public static readonly User DemoUser = new("Jean Dupont", "jean.dupont@mail.com", "+237 6 90 12 34 56");

    public static readonly IReadOnlyList<string> Cities =
        ["Douala", "Yaoundé", "Bafoussam", "Kribi", "Limbé", "Bamenda", "Garoua", "Ngaoundéré"];

    private record Schedule(string Company, int Hour, int Minute, int DurationMinutes, int Price, int Stops, int[] Booked);

    private static readonly Schedule[] Schedules =
    [
        new("Express Voyages", 7, 30, 315, 8500, 0, [1, 2, 5, 6, 7, 10, 13, 14, 17, 22, 25, 26]),
        new("Général Express", 9, 0, 310, 9000, 0, Enumerable.Range(1, 23).ToArray()),
        new("Touristique Express", 11, 30, 320, 8500, 0, Enumerable.Range(1, 28).ToArray()),
        new("Buca Voyages", 14, 0, 320, 8000, 1, [3, 4, 9, 10, 15, 16, 27, 28]),
    ];

    /// <summary>Trajets d'une journée. Aucun départ le dimanche (pour l'état « vide »).</summary>
    public static List<Trip> TripsFor(string from, string to, DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday) return [];
        var day = date.Date;
        return Schedules.Select(s =>
        {
            var departure = day.AddHours(s.Hour).AddMinutes(s.Minute);
            return new Trip(
                Id: $"{from}-{to}-{day:yyyyMMdd}-{s.Company}",
                Company: s.Company,
                From: from,
                To: to,
                Departure: departure,
                Arrival: departure.AddMinutes(s.DurationMinutes),
                Price: s.Price,
                Stops: s.Stops,
                BookedSeats: s.Booked);
        }).ToList();
    }

    /// <summary>Billets déjà achetés au démarrage : un à venir, un passé.</summary>
    public static List<Booking> InitialBookings()
    {
        var now = DateTime.Now;
        var upcoming = TripsFor("Yaoundé", "Douala", SkipSunday(now.AddDays(4)))[0];
        var past = TripsFor("Douala", "Yaoundé", SkipSunday(now.AddDays(-10)))[0];
        return
        [
            new("BT-52904", upcoming, [7], DemoUser.FullName, PaymentMethod.MobileMoney, ServiceFee),
            new("BT-48271", past, [12], DemoUser.FullName, PaymentMethod.MobileMoney, ServiceFee),
        ];
    }

    private static DateTime SkipSunday(DateTime d) => d.DayOfWeek == DayOfWeek.Sunday ? d.AddDays(1) : d;
}

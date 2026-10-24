namespace BusTicket.Core;

/// <summary>
/// Réseau de lignes : villes, compagnies et horaires.
/// Les départs sont calculés (pas stockés) : un identifiant de trajet suffit pour retrouver
/// le trajet, côté serveur comme côté téléphone. Les sièges déjà vendus à d'autres voyageurs
/// sont simulés de façon déterministe ; ceux vendus par BusTicket viennent de la base.
/// </summary>
public static class Catalog
{
    public const string Classic = "Classique";
    public const string Vip = "VIP";

    public const int ServiceFee = 200;
    public const int MaxSeatsPerBooking = 6;

    /// <summary>Plus de vente quand le bus part dans moins de 10 minutes.</summary>
    public static readonly TimeSpan SalesCloseBeforeDeparture = TimeSpan.FromMinutes(10);

    private sealed record City(string Name, string Code, double Lat, double Lon);

    private static readonly City[] CityList =
    [
        new("Douala", "DLA", 4.05, 9.70),
        new("Yaoundé", "YDE", 3.87, 11.52),
        new("Bafoussam", "BFS", 5.48, 10.42),
        new("Bamenda", "BDA", 5.96, 10.15),
        new("Buea", "BUE", 4.16, 9.24),
        new("Limbé", "LMB", 4.02, 9.20),
        new("Kribi", "KRI", 2.94, 9.91),
        new("Ebolowa", "EBW", 2.90, 11.15),
        new("Bertoua", "BTA", 4.58, 13.68),
        new("Ngaoundéré", "NGE", 7.32, 13.58),
        new("Garoua", "GOU", 9.30, 13.40),
        new("Maroua", "MVR", 10.59, 14.32),
    ];

    public static IReadOnlyList<string> Cities { get; } = CityList.Select(c => c.Name).ToList();

    private sealed record Departure(string Company, string ServiceClass, int Hour, int Minute, int Stops);

    private static readonly Departure[] Timetable =
    [
        new("Express Voyages", Classic, 6, 30, 0),
        new("Général Express", Vip, 7, 30, 0),
        new("Touristique Express", Classic, 9, 0, 1),
        new("Buca Voyages", Vip, 11, 0, 0),
        new("United Express", Classic, 13, 30, 1),
        new("Finexs Voyages", Vip, 15, 0, 0),
        new("Express Voyages", Classic, 17, 30, 0),
        new("Général Express", Classic, 21, 0, 0),
    ];

    /// <summary>Le dimanche, les premiers départs de la matinée ne circulent pas.</summary>
    private const int FirstSundayDeparture = 3;

    private static readonly IReadOnlyList<string> ClassicAmenities = ["Climatisation", "Bagage 20 kg"];
    private static readonly IReadOnlyList<string> VipAmenities = ["Climatisation", "Wi-Fi", "Prises USB", "Collation", "Bagage 30 kg"];

    public static bool IsCity(string? name) => CityList.Any(c => c.Name == name);

    /// <summary>Départs d'une journée, dans l'ordre. Sans les sièges vendus par BusTicket (voir BookingEngine).</summary>
    public static IReadOnlyList<TripDto> TripsFor(string from, string to, DateTime date)
    {
        var a = CityList.FirstOrDefault(c => c.Name == from);
        var b = CityList.FirstOrDefault(c => c.Name == to);
        if (a is null || b is null || a == b) return [];

        var first = date.DayOfWeek == DayOfWeek.Sunday ? FirstSundayDeparture : 0;
        return Enumerable.Range(first, Timetable.Length - first).Select(i => Build(a, b, date.Date, i)).ToList();
    }

    /// <summary>Retrouve un trajet à partir de son identifiant « DLA-YDE-20261028-3 ».</summary>
    public static TripDto? FindTrip(string id)
    {
        var parts = id.Split('-');
        if (parts.Length != 4) return null;
        var a = CityList.FirstOrDefault(c => c.Code == parts[0]);
        var b = CityList.FirstOrDefault(c => c.Code == parts[1]);
        if (a is null || b is null || a == b) return null;
        if (!DateTime.TryParseExact(parts[2], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var day)) return null;
        if (!int.TryParse(parts[3], out var index) || index < 0 || index >= Timetable.Length) return null;
        if (day.DayOfWeek == DayOfWeek.Sunday && index < FirstSundayDeparture) return null;
        return Build(a, b, day, index);
    }

    private static TripDto Build(City a, City b, DateTime day, int index)
    {
        var d = Timetable[index];
        var id = $"{a.Code}-{b.Code}-{day:yyyyMMdd}-{index}";
        var km = RoadDistanceKm(a, b);
        var vip = d.ServiceClass == Vip;

        // 60 km/h de moyenne + 20 min par arrêt ; prix au kilomètre, arrondi à 500 FCFA.
        var minutes = (int)Math.Round(km / 60.0 * 60 / 5) * 5 + d.Stops * 20;
        var perKm = vip ? 45 : 28;
        var price = Math.Max(2000, (int)Math.Round(km * perKm / 500.0) * 500);

        var totalSeats = vip ? 21 : 28;   // VIP : 2 + 1 par rangée ; Classique : 2 + 2
        var departure = day.AddHours(d.Hour).AddMinutes(d.Minute);

        return new TripDto(
            Id: id,
            Company: d.Company,
            ServiceClass: d.ServiceClass,
            From: a.Name,
            To: b.Name,
            Departure: departure,
            Arrival: departure.AddMinutes(minutes),
            Price: price,
            Stops: d.Stops,
            TotalSeats: totalSeats,
            SeatsPerRow: vip ? 3 : 4,
            BookedSeats: SimulatedOccupancy(id, totalSeats),
            Amenities: vip ? VipAmenities : ClassicAmenities);
    }

    /// <summary>Distance routière approchée : distance à vol d'oiseau × 1,25.</summary>
    private static double RoadDistanceKm(City a, City b)
    {
        const double earthRadius = 6371;
        static double Rad(double deg) => deg * Math.PI / 180;
        var dLat = Rad(b.Lat - a.Lat);
        var dLon = Rad(b.Lon - a.Lon);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(a.Lat)) * Math.Cos(Rad(b.Lat)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * earthRadius * Math.Asin(Math.Sqrt(h)) * 1.25;
    }

    /// <summary>
    /// Sièges vendus aux guichets des compagnies : toujours les mêmes pour un trajet donné.
    /// Environ un départ sur neuf est complet.
    /// </summary>
    private static IReadOnlyList<int> SimulatedOccupancy(string tripId, int totalSeats)
    {
        var random = new Random(StableHash(tripId));
        if (random.Next(9) == 0) return Enumerable.Range(1, totalSeats).ToList();

        var ratio = 0.15 + random.NextDouble() * 0.6;
        return Enumerable.Range(1, totalSeats)
            .OrderBy(_ => random.Next())
            .Take((int)(totalSeats * ratio))
            .Order()
            .ToList();
    }

    /// <summary>Hachage FNV-1a : identique d'un lancement à l'autre (contrairement à string.GetHashCode).</summary>
    internal static int StableHash(string s)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var c in s) hash = (hash ^ c) * 16777619;
            return (int)hash;
        }
    }
}

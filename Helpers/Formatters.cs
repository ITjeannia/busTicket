namespace BusTicket.Helpers;

/// <summary>Mise en forme des dates, heures, prix et libellés en français.</summary>
public static class Formatters
{
    private static readonly string[] Days = ["dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi"];
    private static readonly string[] Months =
        ["janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre"];
    private static readonly string[] ShortMonths =
        ["janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc."];

    private static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s[1..];
    private static string ShortDay(DateTime d) => Capitalize(Days[(int)d.DayOfWeek][..3]) + ".";

    /// <summary>Jeu. 8</summary>
    public static string DayChip(DateTime d) => $"{ShortDay(d)} {d.Day}";

    /// <summary>Jeu. 8 oct.</summary>
    public static string ShortDate(DateTime d) => $"{ShortDay(d)} {d.Day} {ShortMonths[d.Month - 1]}";

    /// <summary>8 oct.</summary>
    public static string DayMonth(DateTime d) => $"{d.Day} {ShortMonths[d.Month - 1]}";

    /// <summary>jeudi 8 octobre</summary>
    public static string DayName(DateTime d) => $"{Days[(int)d.DayOfWeek]} {d.Day} {Months[d.Month - 1]}";

    /// <summary>Jeudi 8 octobre 2026</summary>
    public static string LongDate(DateTime d) => $"{Capitalize(DayName(d))} {d.Year}";

    /// <summary>07:30</summary>
    public static string Time(DateTime d) => d.ToString("HH:mm");

    /// <summary>5 h 15</summary>
    public static string Duration(TimeSpan d) => $"{(int)d.TotalHours} h {d.Minutes:00}";

    /// <summary>8 500 FCFA (espaces insécables pour ne pas couper le prix)</summary>
    public static string Price(int amount)
    {
        var digits = amount.ToString();
        var chars = new List<char>();
        for (var i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0) chars.Add(' ');
            chars.Add(digits[i]);
        }
        return new string(chars.ToArray()) + " FCFA";
    }

    /// <summary>1 adulte / 2 adultes</summary>
    public static string Adults(int n) => $"{n} adulte{(n > 1 ? "s" : "")}";

    /// <summary>1 passager / 2 passagers</summary>
    public static string Passengers(int n) => $"{n} passager{(n > 1 ? "s" : "")}";

    /// <summary>Siège 12 / Sièges 3, 4</summary>
    public static string Seats(IReadOnlyList<int> seats) =>
        $"{(seats.Count > 1 ? "Sièges" : "Siège")} {string.Join(", ", seats)}";
}

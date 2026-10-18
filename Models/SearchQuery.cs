namespace BusTicket.Models;

/// <summary>Critères saisis sur l'écran de recherche.</summary>
public record SearchQuery(string From, string To, DateTime Date, int Passengers)
{
    public string Route => $"{From} → {To}";
}

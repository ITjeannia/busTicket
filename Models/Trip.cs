namespace BusTicket.Models;

/// <summary>Un trajet proposé par une compagnie.</summary>
public record Trip(
    string Id,
    string Company,
    string From,
    string To,
    DateTime Departure,
    DateTime Arrival,
    int Price,                       // prix d'une place, en FCFA
    int Stops,                       // arrêts intermédiaires (0 = direct)
    IReadOnlyList<int> BookedSeats,  // numéros des sièges occupés (commencent à 1)
    int TotalSeats = 28)
{
    public TimeSpan Duration => Arrival - Departure;
    public int AvailableSeats => TotalSeats - BookedSeats.Count;
    public bool IsFull => AvailableSeats <= 0;
    public string Route => $"{From} → {To}";

    public string StopsLabel => Stops switch
    {
        0 => "Direct",
        1 => "1 arrêt",
        _ => $"{Stops} arrêts",
    };
}

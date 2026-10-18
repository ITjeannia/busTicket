using BusTicket.Models;

namespace BusTicket.Services;

/// <summary>Recherche des trajets (simulée).</summary>
public class TripService
{
    public async Task<List<Trip>> SearchAsync(string from, string to, DateTime date)
    {
        await Task.Delay(800);
        if (MockData.SimulateNetworkError) throw new AppException("Connexion impossible");
        return MockData.TripsFor(from, to, date);
    }
}

namespace BusTicket.Models;

/// <summary>Utilisateur connecté.</summary>
public record User(string FullName, string Email, string Phone)
{
    public string FirstName => FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    /// <summary>« Jean Dupont » → « JD »</summary>
    public string Initials => string.Concat(
        FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));
}

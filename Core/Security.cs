using System.Security.Cryptography;
using System.Text;

namespace BusTicket.Core;

/// <summary>Mots de passe : PBKDF2-SHA256, sel aléatoire. Format stocké : « iterations.sel.hash » (base64).</summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// Contenu du QR code : « BT1|référence|trajet|sièges|signature ».
/// La signature (HMAC-SHA256) empêche de fabriquer un faux billet ; elle se vérifie
/// sans réseau, ce qui permet le contrôle hors ligne.
/// </summary>
public static class TicketSigner
{
    private const string Version = "BT1";

    // Clé de démonstration partagée par le serveur et l'application (contrôle hors ligne).
    // En production : signature asymétrique, clé privée sur le serveur, clé publique dans l'application.
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("BusTicket-demo-signing-key-2026");

    public sealed record Payload(string Reference, string TripId, IReadOnlyList<int> Seats);

    public static string Sign(string reference, string tripId, IReadOnlyList<int> seats)
    {
        var body = $"{Version}|{reference}|{tripId}|{string.Join(',', seats)}";
        return $"{body}|{Signature(body)}";
    }

    /// <summary>Renvoie null si le texte n'est pas un billet BusTicket authentique.</summary>
    public static Payload? Read(string text)
    {
        var parts = text.Trim().Split('|');
        if (parts.Length != 5 || parts[0] != Version) return null;

        var body = string.Join('|', parts[..4]);
        var expected = Encoding.ASCII.GetBytes(Signature(body));
        var actual = Encoding.ASCII.GetBytes(parts[4]);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return null;

        var seats = new List<int>();
        foreach (var s in parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(s, out var seat)) return null;
            seats.Add(seat);
        }
        return new Payload(parts[1], parts[2], seats);
    }

    private static string Signature(string body)
    {
        var mac = HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes(body));
        return Convert.ToBase64String(mac, 0, 12).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

/// <summary>Jeton de session « idUtilisateur.expiration.signature », signé par le serveur.</summary>
public sealed class SessionTokens(string secret, TimeSpan lifetime)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(secret);

    public string Issue(string userId)
    {
        var expires = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var body = $"{userId}.{expires}";
        return $"{body}.{Sign(body)}";
    }

    /// <summary>Renvoie l'identifiant de l'utilisateur, ou null si le jeton est invalide ou expiré.</summary>
    public string? Validate(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var parts = token.Split('.');
        if (parts.Length != 3 || !long.TryParse(parts[1], out var expires)) return null;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires) return null;

        var expected = Encoding.ASCII.GetBytes(Sign($"{parts[0]}.{parts[1]}"));
        return CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(parts[2])) ? parts[0] : null;
    }

    private string Sign(string body) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(body)));
}

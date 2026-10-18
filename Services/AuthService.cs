using BusTicket.Helpers;
using BusTicket.Models;

namespace BusTicket.Services;

/// <summary>
/// Connexion / inscription (simulées).
/// Règle de démo : tout mot de passe de 8 caractères avec un chiffre est accepté.
/// </summary>
public class AuthService
{
    public const int MaxAttempts = 3;

    private int _remainingAttempts = MaxAttempts;

    public User? CurrentUser { get; private set; }

    public async Task LoginAsync(string identifier, string password)
    {
        await Task.Delay(900);
        if (Validators.Password(password) is not null)
        {
            _remainingAttempts = Math.Max(0, _remainingAttempts - 1);
            var plural = _remainingAttempts > 1 ? "s" : "";
            throw new AppException($"E-mail ou mot de passe incorrect. Il vous reste {_remainingAttempts} essai{plural}.");
        }
        _remainingAttempts = MaxAttempts;
        CurrentUser = MockData.DemoUser;
    }

    public async Task RegisterAsync(User user, string password)
    {
        await Task.Delay(900);
        CurrentUser = user;
    }

    public void Logout() => CurrentUser = null;
}

using System.Text.RegularExpressions;

namespace BusTicket.Core;

/// <summary>
/// Règles de validation des formulaires, partagées entre écrans.
/// Chaque méthode renvoie le message d'erreur, ou null si la valeur est valide.
/// </summary>
public static partial class Validators
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    private static int DigitCount(string? value) => (value ?? "").Count(char.IsDigit);

    public static string? Required(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Ce champ est obligatoire" : null;

    public static string? Email(string? value) =>
        EmailRegex().IsMatch((value ?? "").Trim()) ? null : "Adresse e-mail invalide";

    public static string? Phone(string? value) =>
        DigitCount(value) >= 9 ? null : "Numéro de téléphone invalide";

    /// <summary>E-mail ou téléphone (écran de connexion).</summary>
    public static string? EmailOrPhone(string? value) =>
        Email(value) is null || Phone(value) is null ? null : "Saisissez un e-mail ou un numéro valide";

    public static string? Password(string? value)
    {
        var v = value ?? "";
        return v.Length >= 8 && v.Any(char.IsDigit) ? null : "8 caractères minimum, dont 1 chiffre";
    }

    public static string? CardNumber(string? value) =>
        DigitCount(value) >= 16 ? null : "Numéro de carte invalide";
}

using BusTicket.Core;
using BusTicket.Helpers;

namespace BusTicket.Models;

/// <summary>Moyen de paiement proposé à l'écran, avec le champ à remplir et la couleur de l'opérateur.</summary>
public record PaymentMethod(
    PaymentMethodKind Kind,
    string Label,
    string Description,
    string Icon,
    Color Accent,
    string FieldLabel,
    string FieldIcon,
    string FieldPlaceholder,
    Keyboard Keyboard)
{
    public static readonly PaymentMethod OrangeMoney = new(
        PaymentMethodKind.OrangeMoney, "Orange Money", "Paiement depuis votre compte Orange",
        MaterialIcons.Smartphone, Color.FromArgb("#FF7900"),
        "Numéro Orange Money", MaterialIcons.Phone, "6 9X XX XX XX", Keyboard.Telephone);

    public static readonly PaymentMethod MtnMoMo = new(
        PaymentMethodKind.MtnMoMo, "MTN Mobile Money", "Paiement depuis votre compte MoMo",
        MaterialIcons.Smartphone, Color.FromArgb("#E0A800"),
        "Numéro MTN MoMo", MaterialIcons.Phone, "6 7X XX XX XX", Keyboard.Telephone);

    public static readonly PaymentMethod Card = new(
        PaymentMethodKind.Card, "Carte bancaire", "Visa, Mastercard, GIMAC",
        MaterialIcons.CreditCard, Color.FromArgb("#1565C0"),
        "Numéro de carte", MaterialIcons.CreditCard, "1234 5678 9012 3456", Keyboard.Numeric);

    public static readonly IReadOnlyList<PaymentMethod> All = [OrangeMoney, MtnMoMo, Card];
}

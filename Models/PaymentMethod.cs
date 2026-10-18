using BusTicket.Helpers;

namespace BusTicket.Models;

public enum PaymentMethodKind { MobileMoney, Card }

/// <summary>Moyen de paiement proposé, avec le champ à remplir pour chacun.</summary>
public record PaymentMethod(
    PaymentMethodKind Kind,
    string Label,
    string Description,
    string Icon,
    string FieldLabel,
    string FieldIcon,
    string FieldPlaceholder)
{
    public static readonly PaymentMethod MobileMoney = new(
        PaymentMethodKind.MobileMoney, "Mobile Money", "Orange Money, MTN MoMo",
        MaterialIcons.Smartphone, "Numéro Mobile Money", MaterialIcons.Phone, "+237 6 XX XX XX XX");

    public static readonly PaymentMethod Card = new(
        PaymentMethodKind.Card, "Carte bancaire", "Visa, Mastercard",
        MaterialIcons.CreditCard, "Numéro de carte", MaterialIcons.CreditCard, "1234 5678 9012 3456");

    public static readonly IReadOnlyList<PaymentMethod> All = [MobileMoney, Card];
}

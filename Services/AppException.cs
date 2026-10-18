namespace BusTicket.Services;

/// <summary>Erreur « métier » dont le message peut être affiché tel quel à l'utilisateur.</summary>
public class AppException(string message) : Exception(message);

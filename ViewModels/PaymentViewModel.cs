using BusTicket.Core;
using BusTicket.Models;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Un moyen de paiement affiché comme carte sélectionnable.</summary>
public partial class PaymentOption(PaymentMethod method) : ObservableObject
{
    public PaymentMethod Method { get; } = method;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public enum StepState { Pending, Active, Done }

/// <summary>Une étape de l'écran « Paiement en cours ».</summary>
public partial class PaymentStep(string text) : ObservableObject
{
    public string Text { get; } = text;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsPending), nameof(IsActive), nameof(IsDone))]
    public partial StepState State { get; set; }

    public bool IsPending => State == StepState.Pending;
    public bool IsActive => State == StepState.Active;
    public bool IsDone => State == StepState.Done;
}

/// <summary>
/// Paiement. Aucune passerelle n'est branchée : le paiement est simulé et toujours accepté,
/// quel que soit le numéro saisi. Le billet et le reçu sont émis immédiatement.
/// </summary>
public partial class PaymentViewModel(Backend backend, BookingRepository bookings, Navigator navigator)
    : ObservableObject, IQueryAttributable
{
    private TripDto? _trip;
    private List<int> _seats = [];

    public IReadOnlyList<PaymentOption> Options { get; } = PaymentMethod.All.Select(m => new PaymentOption(m)).ToList();

    public IReadOnlyList<PaymentStep> Steps { get; } =
    [
        new("Connexion à l'opérateur"),
        new("Confirmation du paiement"),
        new("Émission du billet et du reçu"),
    ];

    [ObservableProperty] public partial string Route { get; set; } = "";
    [ObservableProperty] public partial string TripDate { get; set; } = "";
    [ObservableProperty] public partial string TripTimes { get; set; } = "";
    [ObservableProperty] public partial string TripDetails { get; set; } = "";
    [ObservableProperty] public partial string TicketLabel { get; set; } = "";
    [ObservableProperty] public partial string TicketPrice { get; set; } = "";
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    public string FeeText { get; } = Formatters.Price(Catalog.ServiceFee);

    [ObservableProperty] public partial string PassengerName { get; set; } = "";
    [ObservableProperty] public partial string? PassengerError { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FieldLabel), nameof(FieldIcon), nameof(FieldPlaceholder), nameof(FieldKeyboard), nameof(FieldHelp))]
    public partial PaymentMethod Method { get; set; } = PaymentMethod.OrangeMoney;

    public string FieldLabel => Method.FieldLabel;
    public string FieldIcon => Method.FieldIcon;
    public string FieldPlaceholder => Method.FieldPlaceholder;
    public Keyboard FieldKeyboard => Method.Keyboard;
    public string FieldHelp => Method.Kind == PaymentMethodKind.Card
        ? "Mode démonstration : aucun montant n'est réellement débité."
        : "Mode démonstration : la demande de confirmation est validée automatiquement.";

    [ObservableProperty] public partial string AccountNumber { get; set; } = "";
    [ObservableProperty] public partial string? FieldError { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsPaying { get; set; }

    public bool IsIdle => !IsPaying;
    public string PayButtonText => $"Payer {TotalText}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _trip = (TripDto)query["trip"];
        _seats = (List<int>)query["seats"];

        Route = _trip.Route;
        TripDate = Formatters.LongDate(_trip.Departure);
        TripTimes = $"{Formatters.Time(_trip.Departure)} → {Formatters.Time(_trip.Arrival)}";
        TripDetails = $"{_trip.Company} · {_trip.ServiceClass} · {Formatters.Seats(_seats)}";
        TicketLabel = $"Billet{(_seats.Count > 1 ? "s" : "")} ({_seats.Count} × {Formatters.Price(_trip.Price)})";
        TicketPrice = Formatters.Price(_trip.Price * _seats.Count);
        TotalText = Formatters.Price(_trip.Price * _seats.Count + Catalog.ServiceFee);
        PassengerName = backend.CurrentUser?.FullName ?? "";
        OnPropertyChanged(nameof(PayButtonText));
        SelectMethod(Options[0]);
    }

    partial void OnAccountNumberChanged(string value) => FieldError = null;
    partial void OnPassengerNameChanged(string value) => PassengerError = null;

    [RelayCommand]
    private void SelectMethod(PaymentOption option)
    {
        foreach (var o in Options) o.IsSelected = o == option;
        Method = option.Method;
        AccountNumber = Method.Kind == PaymentMethodKind.Card ? "" : backend.CurrentUser?.Phone ?? "";
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        // Seule vérification : le champ ne doit pas être vide. Le numéro lui-même n'est jamais refusé.
        PassengerError = Validators.Required(PassengerName);
        FieldError = string.IsNullOrWhiteSpace(AccountNumber) ? "Saisissez le numéro à débiter" : null;
        ErrorMessage = null;
        if (PassengerError is not null || FieldError is not null) return;

        foreach (var step in Steps) step.State = StepState.Pending;
        IsPaying = true;
        try
        {
            var request = new CreateBookingRequest(_trip!.Id, _seats, PassengerName.Trim(), Method.Kind, AccountNumber.Trim());
            var booking = bookings.BookAsync(request);

            // Étapes affichées pendant le traitement (au moins ~2 s pour que l'utilisateur les voie).
            foreach (var step in Steps)
            {
                step.State = StepState.Active;
                await Task.WhenAll(Task.Delay(700), step == Steps[^1] ? booking : Task.CompletedTask);
                step.State = StepState.Done;
            }
            await Task.Delay(250);

            await navigator.ShowReceiptAsync(new Dictionary<string, object> { ["booking"] = await booking });
        }
        catch (BusTicketException e)
        {
            // Seul cas d'échec possible : le siège a été pris entre-temps, ou la vente est fermée.
            ErrorMessage = e.Message;
        }
        finally
        {
            IsPaying = false;
        }
    }
}

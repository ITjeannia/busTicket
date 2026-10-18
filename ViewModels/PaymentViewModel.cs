using BusTicket.Helpers;
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

/// <summary>Maquettes 06 (normal), 06b (refusé), 06c (en cours).</summary>
public partial class PaymentViewModel(AuthService auth, BookingRepository bookings, Navigator navigator)
    : ObservableObject, IQueryAttributable
{
    private Trip? _trip;
    private List<int> _seats = [];

    public IReadOnlyList<PaymentOption> Options { get; } = PaymentMethod.All.Select(m => new PaymentOption(m)).ToList();

    [ObservableProperty] public partial string Route { get; set; } = "";
    [ObservableProperty] public partial string TripDetails { get; set; } = "";
    [ObservableProperty] public partial string TicketLabel { get; set; } = "";
    [ObservableProperty] public partial string TicketPrice { get; set; } = "";
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    public string FeeText { get; } = Formatters.Price(MockData.ServiceFee);

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FieldLabel), nameof(FieldIcon), nameof(FieldPlaceholder))]
    public partial PaymentMethod Method { get; set; } = PaymentMethod.MobileMoney;

    public string FieldLabel => Method.FieldLabel;
    public string FieldIcon => Method.FieldIcon;
    public string FieldPlaceholder => Method.FieldPlaceholder;

    [ObservableProperty] public partial string AccountNumber { get; set; } = "";
    [ObservableProperty] public partial string? FieldError { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsPaying { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PayButtonText), nameof(RefusedMessage))]
    public partial bool WasRefused { get; set; }

    public bool IsIdle => !IsPaying;
    public string PayButtonText => WasRefused ? "Réessayer le paiement" : $"Payer {TotalText}";
    public string? RefusedMessage => WasRefused
        ? "Paiement refusé. Vérifiez vos informations ou choisissez un autre moyen de paiement."
        : null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _trip = (Trip)query["trip"];
        _seats = (List<int>)query["seats"];

        Route = _trip.Route;
        TripDetails = $"{Formatters.ShortDate(_trip.Departure)} · {Formatters.Time(_trip.Departure)} – " +
                      $"{Formatters.Time(_trip.Arrival)} · {Formatters.Seats(_seats)}";
        TicketLabel = $"Billet ({Formatters.Adults(_seats.Count)})";
        TicketPrice = Formatters.Price(_trip.Price * _seats.Count);
        TotalText = Formatters.Price(_trip.Price * _seats.Count + MockData.ServiceFee);
        OnPropertyChanged(nameof(PayButtonText));
        SelectMethod(Options[0]);
    }

    partial void OnAccountNumberChanged(string value) => FieldError = null;

    [RelayCommand]
    private void SelectMethod(PaymentOption option)
    {
        foreach (var o in Options) o.IsSelected = o == option;
        Method = option.Method;
        AccountNumber = Method.Kind == PaymentMethodKind.MobileMoney ? auth.CurrentUser?.Phone ?? "" : "";
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        FieldError = Method.Kind == PaymentMethodKind.MobileMoney
            ? Validators.Phone(AccountNumber)
            : Validators.CardNumber(AccountNumber);
        if (FieldError is not null) return;

        IsPaying = true;
        WasRefused = false;
        try
        {
            var booking = await bookings.PayAsync(_trip!, _seats, auth.CurrentUser?.FullName ?? "", Method, AccountNumber);
            await navigator.ShowNewTicketAsync(new Dictionary<string, object> { ["booking"] = booking });
        }
        catch (AppException e)
        {
            WasRefused = true;
            FieldError = e.Message;
        }
        finally
        {
            IsPaying = false;
        }
    }
}

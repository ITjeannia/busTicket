using BusTicket.Core;
using BusTicket.Helpers;
using BusTicket.Services;
using BusTicket.Theme;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>
/// Espace contrôleur : scan du QR code (caméra) ou saisie de la référence, puis validation de l'embarquement.
/// Un billet validé passe à l'état « embarqué » : un second scan est refusé.
/// </summary>
public partial class ScannerViewModel(Backend backend, BookingRepository bookings, Navigator navigator) : ObservableObject
{
    private string? _lastPayload;

    /// <summary>La caméra cherche un code (faux pendant l'affichage d'un résultat).</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasResult))]
    public partial bool IsScanning { get; set; } = true;

    [ObservableProperty] public partial bool IsChecking { get; set; }
    [ObservableProperty] public partial bool IsBoarding { get; set; }
    [ObservableProperty] public partial string ManualReference { get; set; } = "";
    [ObservableProperty] public partial bool IsOffline { get; set; }

    [ObservableProperty] public partial string ResultTitle { get; set; } = "";
    [ObservableProperty] public partial string ResultMessage { get; set; } = "";
    [ObservableProperty] public partial string ResultIcon { get; set; } = "";
    [ObservableProperty] public partial Color ResultColor { get; set; } = AppColors.Success;
    [ObservableProperty] public partial Color ResultSoftColor { get; set; } = AppColors.SuccessSoft;
    [ObservableProperty] public partial string? TicketRoute { get; set; }
    [ObservableProperty] public partial string? TicketWhen { get; set; }
    [ObservableProperty] public partial string? TicketPassenger { get; set; }
    [ObservableProperty] public partial string? TicketSeats { get; set; }
    [ObservableProperty] public partial string? TicketReference { get; set; }
    [ObservableProperty] public partial bool HasTicketDetails { get; set; }
    [ObservableProperty] public partial bool CanBoard { get; set; }

    public bool HasResult => !IsScanning;

    /// <summary>Résultat affiché (la page joue une animation et une vibration).</summary>
    public event EventHandler<VerificationResult>? ResultShown;

    /// <summary>Appelé par la caméra à chaque code lu.</summary>
    public async Task OnCodeDetectedAsync(string payload)
    {
        if (!IsScanning || IsChecking || string.IsNullOrWhiteSpace(payload)) return;
        await CheckAsync(payload);
    }

    [RelayCommand]
    private async Task VerifyManualAsync()
    {
        var reference = ManualReference.Trim().ToUpperInvariant();
        if (reference.Length == 0) return;
        if (!reference.StartsWith("BT-")) reference = "BT-" + reference;
        await CheckAsync(reference);
    }

    private async Task CheckAsync(string payload)
    {
        IsChecking = true;
        try
        {
            _lastPayload = payload;
            Show(await backend.VerifyAsync(payload, board: false));
        }
        catch (BusTicketException e)
        {
            await navigator.InfoAsync(e.Message);
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private async Task BoardAsync()
    {
        if (_lastPayload is null) return;
        IsBoarding = true;
        try
        {
            Show(await backend.VerifyAsync(_lastPayload, board: true));
            CanBoard = false;
            _ = bookings.RefreshAsync();   // si c'est un billet de cet appareil, il passe à « Embarqué »
        }
        catch (BusTicketException e)
        {
            await navigator.InfoAsync(e.Message);
        }
        finally
        {
            IsBoarding = false;
        }
    }

    [RelayCommand]
    private void ScanAgain()
    {
        ManualReference = "";
        _lastPayload = null;
        IsScanning = true;
    }

    private void Show(VerificationDto result)
    {
        IsOffline = backend.IsOffline;
        (ResultColor, ResultSoftColor, ResultIcon) = result.Result switch
        {
            VerificationResult.Valid or VerificationResult.ValidOffline =>
                (AppColors.Success, AppColors.SuccessSoft, MaterialIcons.CheckCircleFilled),
            VerificationResult.WrongDay or VerificationResult.AlreadyBoarded =>
                (AppColors.Warning, AppColors.WarningSoft, MaterialIcons.WarningFilled),
            _ => (AppColors.Error, AppColors.ErrorSoft, MaterialIcons.CancelFilled),
        };
        ResultTitle = result.Title;
        ResultMessage = result.Message;
        CanBoard = result.CanBoard && result.BoardedAt is null && result.Result == VerificationResult.Valid;

        HasTicketDetails = result.Trip is not null;
        TicketReference = result.Reference;
        TicketRoute = result.Trip?.Route;
        TicketWhen = result.Trip is { } t ? $"{Formatters.ShortDate(t.Departure)} · {Formatters.Time(t.Departure)} · {t.Company}" : null;
        TicketPassenger = result.PassengerName ?? "Nom non disponible hors ligne";
        TicketSeats = result.Seats.Count > 0 ? Formatters.Seats(result.Seats) : null;

        IsScanning = false;
        ResultShown?.Invoke(this, result.Result);
    }
}

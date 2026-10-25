using System.Collections.ObjectModel;
using BusTicket.Services;
using BusTicket.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Onglet « Mes billets » : à venir / passés, tirer pour actualiser.</summary>
public partial class MyTicketsViewModel : ObservableObject
{
    private readonly BookingRepository _bookings;
    private readonly Navigator _navigator;

    public MyTicketsViewModel(BookingRepository bookings, Navigator navigator)
    {
        _bookings = bookings;
        _navigator = navigator;
        _bookings.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        Refresh();
    }

    public IList<string> TabLabels { get; } = ["À venir", "Passés"];

    /// <summary>0 = à venir, 1 = passés.</summary>
    [ObservableProperty] public partial int SelectedTab { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasTickets))]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial string CountText { get; set; } = "";

    public bool HasTickets => !IsEmpty;
    public bool IsUpcoming => SelectedTab == 0;
    public string EmptyTitle => IsUpcoming ? "Aucun billet à venir" : "Aucun billet passé";
    public string EmptyMessage => IsUpcoming
        ? "Réservez votre prochain trajet en quelques étapes. Vos billets apparaîtront ici, même hors connexion."
        : "Vos trajets effectués apparaîtront ici.";
    public string? EmptyActionText => IsUpcoming ? "Rechercher un trajet" : null;

    public ObservableCollection<TicketItem> Tickets { get; } = [];

    partial void OnSelectedTabChanged(int value) => Refresh();

    /// <summary>Recharge la liste (appelé aussi à chaque affichage : un billet a pu expirer).</summary>
    public void Refresh()
    {
        Tickets.Clear();
        var list = IsUpcoming ? _bookings.Upcoming : _bookings.Past;
        for (var i = 0; i < list.Count; i++) Tickets.Add(new TicketItem(list[i], i));
        IsEmpty = Tickets.Count == 0;
        CountText = $"{_bookings.Upcoming.Count} à venir · {_bookings.Past.Count} passé{(_bookings.Past.Count > 1 ? "s" : "")}";
        OnPropertyChanged(nameof(IsUpcoming));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(EmptyActionText));
    }

    /// <summary>Tirer pour actualiser : récupère les billets depuis le serveur.</summary>
    [RelayCommand]
    private async Task ReloadAsync()
    {
        try { await _bookings.RefreshAsync(); }
        finally
        {
            IsRefreshing = false;
            Refresh();
        }
    }

    [RelayCommand]
    private Task OpenTicketAsync(TicketItem item) =>
        _navigator.GoToAsync(Routes.Ticket, new Dictionary<string, object> { ["booking"] = item.Booking });

    [RelayCommand]
    private Task GoToSearchAsync() => _navigator.GoToAsync(Routes.Search);
}

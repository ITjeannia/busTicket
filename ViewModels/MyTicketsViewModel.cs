using System.Collections.ObjectModel;
using BusTicket.Helpers;
using BusTicket.Services;
using BusTicket.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Onglet « Mes billets » — maquettes 07 et 07b.</summary>
public partial class MyTicketsViewModel : ObservableObject
{
    private readonly BookingRepository _bookings;
    private readonly Navigator _navigator;

    public MyTicketsViewModel(BookingRepository bookings, Navigator navigator)
    {
        _bookings = bookings;
        _navigator = navigator;
        _bookings.Changed += (_, _) => Refresh();
        Refresh();
    }

    public IList<string> TabLabels { get; } = ["À venir", "Passés"];

    /// <summary>0 = à venir, 1 = passés.</summary>
    [ObservableProperty] public partial int SelectedTab { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasTickets))]
    public partial bool IsEmpty { get; set; }

    public bool HasTickets => !IsEmpty;
    public bool IsUpcoming => SelectedTab == 0;
    public string? InfoMessage => IsUpcoming ? "Les billets restent consultables sans connexion internet." : null;
    public string EmptyTitle => IsUpcoming ? "Aucun billet à venir" : "Aucun billet passé";
    public string EmptyMessage => IsUpcoming
        ? "Réservez votre prochain trajet en quelques étapes. Vos billets apparaîtront ici, même hors connexion."
        : "Vos trajets effectués apparaîtront ici.";
    public string? EmptyActionText => IsUpcoming ? "Rechercher un trajet" : null;
    public string TicketIcon => MaterialIcons.Ticket;

    public ObservableCollection<TicketItem> Tickets { get; } = [];

    partial void OnSelectedTabChanged(int value) => Refresh();

    /// <summary>Recharge la liste (appelé aussi à chaque affichage : un billet a pu expirer).</summary>
    public void Refresh()
    {
        Tickets.Clear();
        foreach (var booking in IsUpcoming ? _bookings.Upcoming : _bookings.Past) Tickets.Add(new TicketItem(booking));
        IsEmpty = Tickets.Count == 0;
        OnPropertyChanged(nameof(IsUpcoming));
        OnPropertyChanged(nameof(InfoMessage));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(EmptyActionText));
    }

    [RelayCommand]
    private Task OpenTicketAsync(TicketItem item) =>
        _navigator.GoToAsync(Routes.Ticket, new Dictionary<string, object> { ["booking"] = item.Booking });

    [RelayCommand]
    private Task GoToSearchAsync() => _navigator.GoToAsync(Routes.Search);
}

using System.Collections.ObjectModel;
using BusTicket.Core;
using BusTicket.Models;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Onglet « Rechercher » : formulaire, trajets populaires, recherches récentes, prochain voyage.</summary>
public partial class SearchViewModel : ObservableObject
{
    private const int MaxPassengers = Catalog.MaxSeatsPerBooking;

    private readonly Backend _backend;
    private readonly BookingRepository _bookings;
    private readonly SearchHistory _history;
    private readonly Navigator _navigator;

    public SearchViewModel(Backend backend, BookingRepository bookings, SearchHistory history, Navigator navigator)
    {
        _backend = backend;
        _bookings = bookings;
        _history = history;
        _navigator = navigator;
        _history.Recent.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRecent));
            OnPropertyChanged(nameof(HasNoRecent));
        };
        _bookings.Changed += (_, _) => RefreshNextTrip();
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArrivalError), nameof(CanSearch))]
    public partial string? From { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArrivalError), nameof(CanSearch))]
    public partial string? To { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsToday), nameof(IsTomorrow))]
    public partial DateTime Date { get; set; } = DateTime.Today;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PassengersText))]
    public partial int Passengers { get; set; } = 1;

    [ObservableProperty] public partial string Greeting { get; set; } = "Bonjour";
    [ObservableProperty] public partial string Initials { get; set; } = "";

    /// <summary>Prochain voyage (carte « Votre prochain départ »), null s'il n'y en a pas.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasNextTrip))]
    public partial BookingDto? NextTrip { get; set; }

    [ObservableProperty] public partial string NextTripRoute { get; set; } = "";
    [ObservableProperty] public partial string NextTripWhen { get; set; } = "";

    public bool HasNextTrip => NextTrip is not null;
    public bool IsToday => Date.Date == DateTime.Today;
    public bool IsTomorrow => Date.Date == DateTime.Today.AddDays(1);
    public string PassengersText => Formatters.Adults(Passengers);
    public string? ArrivalError => From is not null && From == To ? "Choisissez une ville différente du départ" : null;
    public bool CanSearch => From is not null && To is not null && ArrivalError is null;

    public ObservableCollection<RecentSearch> Recent => _history.Recent;
    public bool HasRecent => Recent.Count > 0;
    public bool HasNoRecent => !HasRecent;

    /// <summary>Lignes les plus demandées, en raccourci.</summary>
    public IReadOnlyList<RecentSearch> PopularRoutes { get; } =
    [
        new("Douala", "Yaoundé"),
        new("Yaoundé", "Douala"),
        new("Douala", "Bafoussam"),
        new("Douala", "Kribi"),
        new("Yaoundé", "Ngaoundéré"),
        new("Douala", "Buea"),
    ];

    /// <summary>Appelé par la page à chaque affichage (l'utilisateur a pu changer).</summary>
    public void Refresh()
    {
        var user = _backend.CurrentUser;
        var hour = DateTime.Now.Hour;
        var hello = hour is >= 5 and < 18 ? "Bonjour" : "Bonsoir";
        Greeting = $"{hello} {user?.FirstName}".Trim();
        Initials = user?.Initials ?? "";
        if (Date.Date < DateTime.Today) Date = DateTime.Today;
        RefreshNextTrip();
    }

    private void RefreshNextTrip()
    {
        NextTrip = _bookings.Upcoming.FirstOrDefault(b => !b.IsBoarded);
        if (NextTrip is null) return;
        NextTripRoute = NextTrip.Trip.Route;
        NextTripWhen = $"{Formatters.ShortDate(NextTrip.Trip.Departure)} · {Formatters.Time(NextTrip.Trip.Departure)} · {Formatters.Seats(NextTrip.Seats)}";
    }

    [RelayCommand]
    private async Task PickFromAsync() => From = await _navigator.PickAsync("Ville de départ", _backend.Cities) ?? From;

    [RelayCommand]
    private async Task PickToAsync() =>
        To = await _navigator.PickAsync("Ville d'arrivée", _backend.Cities.Where(c => c != From)) ?? To;

    [RelayCommand]
    private async Task PickPassengersAsync()
    {
        var options = Enumerable.Range(1, MaxPassengers).Select(Formatters.Adults).ToList();
        var choice = await _navigator.PickAsync("Passagers", options);
        if (choice is not null) Passengers = options.IndexOf(choice) + 1;
    }

    [RelayCommand]
    private void SetToday() => Date = DateTime.Today;

    [RelayCommand]
    private void SetTomorrow() => Date = DateTime.Today.AddDays(1);

    [RelayCommand]
    private void Swap() => (From, To) = (To, From);

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!CanSearch) return;
        _history.Add(From!, To!);
        await _navigator.GoToAsync(Routes.Results, new Dictionary<string, object>
        {
            ["query"] = new SearchQuery(From!, To!, Date, Passengers),
        });
    }

    [RelayCommand]
    private Task SearchRecentAsync(RecentSearch recent)
    {
        From = recent.From;
        To = recent.To;
        return SearchAsync();
    }

    [RelayCommand]
    private Task OpenNextTripAsync() =>
        NextTrip is null
            ? Task.CompletedTask
            : _navigator.ShowTicketAsync(new Dictionary<string, object> { ["booking"] = NextTrip });
}

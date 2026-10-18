using System.Collections.ObjectModel;
using BusTicket.Helpers;
using BusTicket.Models;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Onglet « Rechercher » — maquettes 03 et 03b.</summary>
public partial class SearchViewModel : ObservableObject
{
    private const int MaxPassengers = 6;

    private readonly AuthService _auth;
    private readonly SearchHistory _history;
    private readonly Navigator _navigator;

    public SearchViewModel(AuthService auth, SearchHistory history, Navigator navigator)
    {
        _auth = auth;
        _history = history;
        _navigator = navigator;
        _history.Recent.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRecent));
            OnPropertyChanged(nameof(HasNoRecent));
        };
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArrivalError), nameof(CanSearch))]
    public partial string? From { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArrivalError), nameof(CanSearch))]
    public partial string? To { get; set; }

    [ObservableProperty] public partial DateTime Date { get; set; } = DateTime.Today;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PassengersText))]
    public partial int Passengers { get; set; } = 1;

    [ObservableProperty] public partial string Greeting { get; set; } = "Bonjour";

    public string PassengersText => Formatters.Adults(Passengers);
    public string? ArrivalError => From is not null && From == To ? "Choisissez une ville différente du départ" : null;
    public bool CanSearch => From is not null && To is not null && ArrivalError is null;

    public ObservableCollection<RecentSearch> Recent => _history.Recent;
    public bool HasRecent => Recent.Count > 0;
    public bool HasNoRecent => !HasRecent;

    /// <summary>Appelé par la page à chaque affichage (l'utilisateur a pu changer).</summary>
    public void Refresh() => Greeting = $"Bonjour {_auth.CurrentUser?.FirstName}".Trim();

    [RelayCommand]
    private async Task PickFromAsync() => From = await _navigator.PickAsync("Ville de départ", MockData.Cities) ?? From;

    [RelayCommand]
    private async Task PickToAsync() => To = await _navigator.PickAsync("Ville d'arrivée", MockData.Cities) ?? To;

    [RelayCommand]
    private async Task PickPassengersAsync()
    {
        var options = Enumerable.Range(1, MaxPassengers).Select(Formatters.Adults).ToList();
        var choice = await _navigator.PickAsync("Passagers", options);
        if (choice is not null) Passengers = options.IndexOf(choice) + 1;
    }

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
}

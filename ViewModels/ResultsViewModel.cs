using System.Collections.ObjectModel;
using BusTicket.Core;
using BusTicket.Models;
using BusTicket.Services;
using BusTicket.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

public enum LoadState { Loading, Loaded, Empty, Error }

/// <summary>Liste des départs : chargement, liste, aucun départ, erreur.</summary>
public partial class ResultsViewModel(Backend backend, Navigator navigator) : ObservableObject, IQueryAttributable
{
    private SearchQuery _query = new("", "", DateTime.Today, 1);
    private List<DateTime> _days = [];
    private bool _isUpdatingDays;

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial string Subtitle { get; set; } = "";
    [ObservableProperty] public partial IList<string> DayLabels { get; set; } = [];
    [ObservableProperty] public partial int SelectedDayIndex { get; set; }
    [ObservableProperty] public partial string CountText { get; set; } = "";
    [ObservableProperty] public partial string EmptyMessage { get; set; } = "";
    [ObservableProperty] public partial string NextDayText { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial bool IsOffline { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsLoading), nameof(IsLoaded), nameof(IsEmpty), nameof(IsError))]
    public partial LoadState State { get; set; }

    public bool IsLoading => State == LoadState.Loading;
    public bool IsLoaded => State == LoadState.Loaded;
    public bool IsEmpty => State == LoadState.Empty;
    public bool IsError => State == LoadState.Error;

    public ObservableCollection<TripItem> Trips { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("query", out var value) && value is SearchQuery searchQuery)
        {
            _query = searchQuery;
            Title = searchQuery.Route;
            _ = LoadAsync(searchQuery.Date);
        }
    }

    partial void OnSelectedDayIndexChanged(int value)
    {
        if (!_isUpdatingDays && value >= 0 && value < _days.Count) _ = LoadAsync(_days[value]);
    }

    /// <summary>Trois jours affichés : la veille, le jour choisi et le lendemain (jamais avant aujourd'hui).</summary>
    private void UpdateDays(DateTime selected)
    {
        var previous = selected.Date.AddDays(-1);
        var start = previous < DateTime.Today ? DateTime.Today : previous;
        _days = Enumerable.Range(0, 3).Select(i => start.AddDays(i)).ToList();

        _isUpdatingDays = true;
        DayLabels = _days.Select(Formatters.DayChip).ToList();
        SelectedDayIndex = _days.IndexOf(selected.Date);
        _isUpdatingDays = false;
    }

    private async Task LoadAsync(DateTime date)
    {
        _query = _query with { Date = date.Date };
        UpdateDays(_query.Date);
        Subtitle = $"{Formatters.LongDate(_query.Date)} · {Formatters.Passengers(_query.Passengers)}";
        State = LoadState.Loading;
        Trips.Clear();
        try
        {
            var started = DateTime.Now;
            var results = await backend.SearchAsync(_query.From, _query.To, _query.Date);
            // Laisse le squelette visible un court instant : évite un clignotement quand la réponse est immédiate.
            var elapsed = DateTime.Now - started;
            if (elapsed < TimeSpan.FromMilliseconds(450)) await Task.Delay(TimeSpan.FromMilliseconds(450) - elapsed);

            var index = 0;
            foreach (var trip in results) Trips.Add(new TripItem(trip, _query.Passengers, index++));
            var available = results.Count(t => t.AvailableSeats >= _query.Passengers);
            CountText = $"{results.Count} départ{(results.Count > 1 ? "s" : "")} · {available} avec assez de places";
            EmptyMessage = _query.Date == DateTime.Today
                ? $"Plus aucun départ en vente aujourd'hui entre {_query.From} et {_query.To}. Essayez demain."
                : $"Aucun bus ne circule entre {_query.From} et {_query.To} le {Formatters.DayName(_query.Date)}.";
            NextDayText = $"Voir le {Formatters.DayName(_query.Date.AddDays(1))}";
            IsOffline = backend.IsOffline;
            State = results.Count == 0 ? LoadState.Empty : LoadState.Loaded;
        }
        catch (BusTicketException e)
        {
            ErrorMessage = e.Message;
            State = LoadState.Error;
        }
    }

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(_query.Date);

    [RelayCommand]
    private Task ShowNextDayAsync() => LoadAsync(_query.Date.AddDays(1));

    [RelayCommand]
    private async Task OpenTripAsync(TripItem item)
    {
        if (item.IsFull) return;
        if (!item.HasEnoughSeats)
        {
            await navigator.InfoAsync($"Il ne reste que {item.Trip.AvailableSeats} place(s) sur ce départ pour {_query.Passengers} passagers.");
            return;
        }
        await navigator.GoToAsync(Routes.Seats, new Dictionary<string, object>
        {
            ["trip"] = item.Trip,
            ["passengers"] = _query.Passengers,
        });
    }
}

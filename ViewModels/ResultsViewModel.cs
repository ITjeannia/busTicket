using System.Collections.ObjectModel;
using BusTicket.Helpers;
using BusTicket.Models;
using BusTicket.Services;
using BusTicket.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

public enum LoadState { Loading, Loaded, Empty, Error }

/// <summary>Maquettes 04 (liste), 04b (chargement), 04c (aucun trajet), 04d (erreur réseau).</summary>
public partial class ResultsViewModel(TripService trips, Navigator navigator) : ObservableObject, IQueryAttributable
{
    private SearchQuery _query = new("", "", DateTime.Today, 1);
    private List<DateTime> _days = [];
    private bool _isUpdatingDays;

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial IList<string> DayLabels { get; set; } = [];
    [ObservableProperty] public partial int SelectedDayIndex { get; set; }
    [ObservableProperty] public partial string CountText { get; set; } = "";
    [ObservableProperty] public partial string EmptyMessage { get; set; } = "";
    [ObservableProperty] public partial string NextDayText { get; set; } = "";

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
        State = LoadState.Loading;
        Trips.Clear();
        try
        {
            var results = await trips.SearchAsync(_query.From, _query.To, _query.Date);
            foreach (var trip in results) Trips.Add(new TripItem(trip));
            CountText = $"{results.Count} départs · {Formatters.Passengers(_query.Passengers)}";
            EmptyMessage = $"Aucun bus ne circule entre {_query.From} et {_query.To} le {Formatters.DayName(_query.Date)}. Essayez une autre date.";
            NextDayText = $"Voir le {Formatters.DayName(_query.Date.AddDays(1))}";
            State = results.Count == 0 ? LoadState.Empty : LoadState.Loaded;
        }
        catch (Exception)
        {
            State = LoadState.Error;
        }
    }

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(_query.Date);

    [RelayCommand]
    private Task ShowNextDayAsync() => LoadAsync(_query.Date.AddDays(1));

    [RelayCommand]
    private Task OpenTripAsync(TripItem item) =>
        item.IsFull
            ? Task.CompletedTask
            : navigator.GoToAsync(Routes.Seats, new Dictionary<string, object>
            {
                ["trip"] = item.Trip,
                ["passengers"] = _query.Passengers,
            });
}

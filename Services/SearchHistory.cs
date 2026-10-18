using System.Collections.ObjectModel;

namespace BusTicket.Services;

/// <summary>Une recherche récente (départ, arrivée).</summary>
public record RecentSearch(string From, string To)
{
    public string Route => $"{From} → {To}";
}

/// <summary>Dernières recherches, la plus récente en premier.</summary>
public class SearchHistory
{
    private const int MaxItems = 5;

    public ObservableCollection<RecentSearch> Recent { get; } = [];

    public void Add(string from, string to)
    {
        var search = new RecentSearch(from, to);
        Recent.Remove(search);
        Recent.Insert(0, search);
        while (Recent.Count > MaxItems) Recent.RemoveAt(Recent.Count - 1);
    }
}

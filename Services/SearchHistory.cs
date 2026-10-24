using System.Collections.ObjectModel;

namespace BusTicket.Services;

/// <summary>Une recherche récente (départ, arrivée).</summary>
public record RecentSearch(string From, string To)
{
    public string Route => $"{From} → {To}";
}

/// <summary>Dernières recherches, la plus récente en premier. Gardées entre deux lancements.</summary>
public class SearchHistory(LocalStore store)
{
    private const int MaxItems = 5;
    private const string StoreName = "recent_searches";

    public ObservableCollection<RecentSearch> Recent { get; } = [.. store.Load<List<RecentSearch>>(StoreName) ?? []];

    public void Add(string from, string to)
    {
        var search = new RecentSearch(from, to);
        Recent.Remove(search);
        Recent.Insert(0, search);
        while (Recent.Count > MaxItems) Recent.RemoveAt(Recent.Count - 1);
        store.Save(StoreName, Recent.ToList());
    }
}

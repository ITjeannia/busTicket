using CommunityToolkit.Mvvm.ComponentModel;

namespace BusTicket.ViewModels.Items;

public enum SeatStatus { Available, Selected, Booked }

/// <summary>Un siège du plan du bus.</summary>
public partial class SeatItem(int number, SeatStatus status) : ObservableObject
{
    public int Number { get; } = number;

    [ObservableProperty] public partial SeatStatus Status { get; set; } = status;
}

/// <summary>
/// Une rangée du bus. Classique : 2 sièges + allée + 2 sièges.
/// VIP : 2 sièges + allée + 1 siège (RightAisle reste vide). null = pas de siège.
/// </summary>
public record SeatRow(SeatItem? LeftWindow, SeatItem? LeftAisle, SeatItem? RightAisle, SeatItem? RightWindow);

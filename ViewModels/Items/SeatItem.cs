using CommunityToolkit.Mvvm.ComponentModel;

namespace BusTicket.ViewModels.Items;

public enum SeatStatus { Available, Selected, Booked }

/// <summary>Un siège du plan du bus.</summary>
public partial class SeatItem(int number, SeatStatus status) : ObservableObject
{
    public int Number { get; } = number;

    [ObservableProperty] public partial SeatStatus Status { get; set; } = status;
}

/// <summary>Une rangée du bus : 2 sièges à gauche de l'allée, 2 à droite (null = pas de siège).</summary>
public record SeatRow(SeatItem? LeftWindow, SeatItem? LeftAisle, SeatItem? RightAisle, SeatItem? RightWindow);

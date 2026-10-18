namespace BusTicket.Theme;

/// <summary>
/// Palette de l'application, relevée sur la maquette Figma.
/// Source unique : utilisée en C# et en XAML via {x:Static theme:AppColors.Primary}.
/// </summary>
public static class AppColors
{
    // Marque
    public static readonly Color Primary = Color.FromArgb("#1565C0");
    public static readonly Color PrimarySoft = Color.FromArgb("#E3F2FD");

    // Fonds
    public static readonly Color Background = Colors.White;
    public static readonly Color Surface = Colors.White;
    public static readonly Color SurfaceMuted = Color.FromArgb("#F1F2F6");
    public static readonly Color Border = Color.FromArgb("#D6D5DF");
    public static readonly Color Disabled = Color.FromArgb("#BDBDBD");

    // Textes
    public static readonly Color TextPrimary = Color.FromArgb("#1C1B1F");
    public static readonly Color TextSecondary = Color.FromArgb("#5F6368");
    public static readonly Color TextDisabled = Color.FromArgb("#9E9E9E");
    public static readonly Color TextOnPrimary = Colors.White;

    // États
    public static readonly Color Error = Color.FromArgb("#B3261E");
    public static readonly Color ErrorSoft = Color.FromArgb("#FCE8E6");
    public static readonly Color Success = Color.FromArgb("#2E7D32");
    public static readonly Color SuccessSoft = Color.FromArgb("#E8F5E9");
}

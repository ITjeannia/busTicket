namespace BusTicket.Theme;

/// <summary>
/// Palette de l'application (couleurs de la maquette Figma, inchangées) + quelques nuances dérivées.
/// Source unique : utilisée en C# et en XAML via {x:Static theme:AppColors.Primary}.
/// </summary>
public static class AppColors
{
    // Marque
    public static readonly Color Primary = Color.FromArgb("#1565C0");
    public static readonly Color PrimaryDark = Color.FromArgb("#0D47A1");
    public static readonly Color PrimarySoft = Color.FromArgb("#E3F2FD");
    public static readonly Color PrimaryMuted = Color.FromArgb("#BBDEFB");

    // Fonds
    public static readonly Color Background = Color.FromArgb("#F5F7FA");
    public static readonly Color Surface = Colors.White;
    public static readonly Color SurfaceMuted = Color.FromArgb("#F1F2F6");
    public static readonly Color Border = Color.FromArgb("#D6D5DF");
    public static readonly Color Divider = Color.FromArgb("#E7E8EE");
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
    public static readonly Color Warning = Color.FromArgb("#B26A00");
    public static readonly Color WarningSoft = Color.FromArgb("#FFF4E5");

    // Écran du scanner
    public static readonly Color ScannerBackdrop = Color.FromArgb("#0B1A2E");
}

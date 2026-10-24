namespace BusTicket.Theme;

/// <summary>Noms des polices déclarées dans MauiProgram.cs et tailles de texte communes.</summary>
public static class AppFonts
{
    public const string Regular = "Roboto";
    public const string Medium = "RobotoMedium";
    public const string Bold = "RobotoBold";
    public const string Icons = "MaterialIcons";

    public const double Display = 32;
    public const double Headline = 26;
    public const double Title = 20;
    public const double Subtitle = 17;
    public const double Body = 16;
    public const double Caption = 14;
    public const double Small = 12;
    public const double Overline = 11;
}

/// <summary>Espacements, rayons et hauteurs communs (maquette en 360 × 800). Rayons courts : style « carré ».</summary>
public static class AppSpacing
{
    public const double Xs = 4;
    public const double Sm = 8;
    public const double Md = 16;
    public const double Lg = 24;
    public const double Xl = 32;

    public const double ScreenPadding = Md;
    public const double ControlHeight = 50;

    public const double RadiusXs = 4;
    public const double RadiusSm = 6;
    public const double RadiusMd = 8;
    public const double RadiusPill = 100;
}

/// <summary>Durées d'animation (ms).</summary>
public static class AppMotion
{
    public const uint Fast = 120;
    public const uint Normal = 260;
    public const uint Slow = 420;
}

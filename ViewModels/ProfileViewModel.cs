using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Onglet « Profil » — maquette 09.</summary>
public partial class ProfileViewModel(AuthService auth, Navigator navigator) : ObservableObject
{
    [ObservableProperty] public partial string FullName { get; set; } = "";
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string Initials { get; set; } = "";

    public string Footer => $"{AppInfo.Name} {AppInfo.VersionString} · Application légère, sans publicité ni traceur";

    /// <summary>Appelé par la page à chaque affichage.</summary>
    public void Refresh()
    {
        FullName = auth.CurrentUser?.FullName ?? "";
        Email = auth.CurrentUser?.Email ?? "";
        Initials = auth.CurrentUser?.Initials ?? "";
    }

    [RelayCommand]
    private Task ComingSoonAsync() => navigator.InfoAsync(Navigator.ComingSoon);

    [RelayCommand]
    private Task LogoutAsync()
    {
        auth.Logout();
        return navigator.GoToAsync(Routes.Login);
    }
}

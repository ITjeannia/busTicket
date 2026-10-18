using BusTicket.Helpers;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Maquettes 01 (normal), 01b (erreur), 01c (chargement).</summary>
public partial class LoginViewModel(AuthService auth, Navigator navigator) : ObservableObject
{
    [ObservableProperty] public partial string Identifier { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? IdentifierError { get; set; }
    [ObservableProperty] public partial string? PasswordError { get; set; }
    [ObservableProperty] public partial string? BannerError { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsLoading { get; set; }

    public bool IsIdle => !IsLoading;

    [RelayCommand]
    private async Task LoginAsync()
    {
        IdentifierError = Validators.EmailOrPhone(Identifier);
        PasswordError = Validators.Required(Password);
        BannerError = null;
        if (IdentifierError is not null || PasswordError is not null) return;

        IsLoading = true;
        try
        {
            await auth.LoginAsync(Identifier.Trim(), Password);
            Password = "";
            await navigator.GoToAsync(Routes.Search);
        }
        catch (AppException e)
        {
            BannerError = e.Message;
            PasswordError = "Mot de passe incorrect";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task GoToRegisterAsync() => navigator.GoToAsync(Routes.Register);

    [RelayCommand]
    private Task ForgotPasswordAsync() => navigator.InfoAsync(Navigator.ComingSoon);
}

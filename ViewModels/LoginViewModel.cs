using BusTicket.Core;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Connexion par e-mail ou téléphone. Compte de démonstration : jean.dupont@mail.com / voyage123.</summary>
public partial class LoginViewModel(Backend backend, BookingRepository bookings, Navigator navigator) : ObservableObject
{
    [ObservableProperty] public partial string Identifier { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? IdentifierError { get; set; }
    [ObservableProperty] public partial string? PasswordError { get; set; }
    [ObservableProperty] public partial string? BannerError { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsLoading { get; set; }

    public bool IsIdle => !IsLoading;

    /// <summary>Déclenche la secousse du formulaire (voir LoginPage).</summary>
    public event EventHandler? Failed;

    partial void OnIdentifierChanged(string value) => IdentifierError = null;
    partial void OnPasswordChanged(string value) => PasswordError = null;

    [RelayCommand]
    private async Task LoginAsync()
    {
        IdentifierError = Validators.EmailOrPhone(Identifier);
        PasswordError = Validators.Required(Password);
        BannerError = null;
        if (IdentifierError is not null || PasswordError is not null)
        {
            Failed?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsLoading = true;
        try
        {
            await backend.LoginAsync(Identifier.Trim(), Password);
            Password = "";
            _ = bookings.RefreshAsync();
            await navigator.GoToAsync(Routes.Search);
        }
        catch (BusTicketException e)
        {
            BannerError = e.Message;
            Failed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Remplit le compte de démonstration.</summary>
    [RelayCommand]
    private void UseDemoAccount()
    {
        Identifier = BookingEngine.DemoEmail;
        Password = BookingEngine.DemoPassword;
    }

    [RelayCommand]
    private Task GoToRegisterAsync() => navigator.GoToAsync(Routes.Register);

    [RelayCommand]
    private Task ForgotPasswordAsync() => navigator.InfoAsync(
        "Contactez le service client BusTicket au +237 6 99 00 00 00 pour réinitialiser votre mot de passe.",
        "Mot de passe oublié");
}

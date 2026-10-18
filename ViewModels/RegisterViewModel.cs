using BusTicket.Helpers;
using BusTicket.Models;
using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Maquette 02. Le bouton reste grisé tant que tout n'est pas rempli et accepté.</summary>
public partial class RegisterViewModel(AuthService auth, Navigator navigator) : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowSubmitHint))] public partial string FullName { get; set; } = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowSubmitHint))] public partial string Email { get; set; } = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowSubmitHint))] public partial string Phone { get; set; } = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowSubmitHint))] public partial string Password { get; set; } = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowSubmitHint))] public partial string Confirm { get; set; } = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanSubmit), nameof(ShowSubmitHint))] public partial bool AcceptedTerms { get; set; }

    [ObservableProperty] public partial string? FullNameError { get; set; }
    [ObservableProperty] public partial string? EmailError { get; set; }
    [ObservableProperty] public partial string? PhoneError { get; set; }
    [ObservableProperty] public partial string? PasswordError { get; set; }
    [ObservableProperty] public partial string? ConfirmError { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }

    public bool CanSubmit => AcceptedTerms &&
        new[] { FullName, Email, Phone, Password, Confirm }.All(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>Affiche l'aide « Remplissez tous les champs… » sous le bouton.</summary>
    public bool ShowSubmitHint => !CanSubmit;


    [RelayCommand]
    private void ToggleTerms() => AcceptedTerms = !AcceptedTerms;

    // Efface l'erreur d'un champ dès que l'utilisateur le corrige.
    partial void OnFullNameChanged(string value) => FullNameError = null;
    partial void OnEmailChanged(string value) => EmailError = null;
    partial void OnPhoneChanged(string value) => PhoneError = null;
    partial void OnPasswordChanged(string value) => PasswordError = null;
    partial void OnConfirmChanged(string value) => ConfirmError = null;

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        FullNameError = Validators.Required(FullName);
        EmailError = Validators.Email(Email);
        PhoneError = Validators.Phone(Phone);
        PasswordError = Validators.Password(Password);
        ConfirmError = Confirm == Password ? null : "Les mots de passe ne correspondent pas";
        if (new[] { FullNameError, EmailError, PhoneError, PasswordError, ConfirmError }.Any(e => e is not null)) return;

        IsLoading = true;
        try
        {
            await auth.RegisterAsync(new User(FullName.Trim(), Email.Trim(), Phone.Trim()), Password);
            await navigator.GoToAsync(Routes.Search);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task BackToLoginAsync() => navigator.BackAsync();
}

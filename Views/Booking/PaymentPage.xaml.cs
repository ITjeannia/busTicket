using System.ComponentModel;
using BusTicket.Theme;
using BusTicket.ViewModels;

namespace BusTicket.Views.Booking;

public partial class PaymentPage : ContentPage
{
    public PaymentPage(PaymentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var vm = (PaymentViewModel)sender!;
        switch (e.PropertyName)
        {
            // Fenêtre « Paiement en cours » : fondu + carte qui monte.
            case nameof(PaymentViewModel.IsPaying) when vm.IsPaying:
                Processing.Opacity = 0;
                ProcessingCard.TranslationY = 40;
                Processing.IsVisible = true;
                await Task.WhenAll(
                    Processing.FadeToAsync(1, AppMotion.Normal),
                    ProcessingCard.TranslateToAsync(0, 0, AppMotion.Slow, Easing.CubicOut));
                break;

            case nameof(PaymentViewModel.IsPaying):
                await Processing.FadeToAsync(0, AppMotion.Fast);
                Processing.IsVisible = false;
                break;

            // Champ vide ou erreur : on fait défiler jusqu'au champ pour que le message soit visible.
            case nameof(PaymentViewModel.FieldError) when vm.FieldError is not null:
                await Scroller.ScrollToAsync(AccountField, ScrollToPosition.Center, true);
                break;

            case nameof(PaymentViewModel.ErrorMessage) when vm.ErrorMessage is not null:
                await Scroller.ScrollToAsync(0, 0, true);
                break;
        }
    }
}

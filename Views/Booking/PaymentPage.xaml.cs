using System.ComponentModel;
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

    /// <summary>Paiement refusé : fait défiler jusqu'au champ pour que son message d'erreur soit visible.</summary>
    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PaymentViewModel.WasRefused) && ((PaymentViewModel)sender!).WasRefused)
            await Scroller.ScrollToAsync(AccountField, ScrollToPosition.Center, true);
    }
}

using BusTicket.ViewModels;

namespace BusTicket.Views.Booking;

public partial class ReceiptPage : ContentPage
{
    private bool _played;

    public ReceiptPage(ReceiptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Halo.Scale = 0.4;
        Halo.Opacity = 0;
        Badge.Scale = 0;
        Tick.Opacity = 0;
        Tick.Rotation = -30;
    }

    /// <summary>Coche : le rond grossit avec un rebond, puis la coche apparaît en pivotant ; le halo respire.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_played) return;
        _played = true;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
        catch (Exception) { /* pas de vibreur */ }

        await Task.WhenAll(
            Halo.FadeToAsync(1, 300),
            Halo.ScaleToAsync(1, 500, Easing.CubicOut),
            Badge.ScaleToAsync(1, 520, Easing.SpringOut));
        await Task.WhenAll(
            Tick.FadeToAsync(1, 200),
            Tick.RotateToAsync(0, 260, Easing.CubicOut));

        var breathe = new Animation
        {
            { 0, 0.5, new Animation(v => Halo.Scale = v, 1, 1.08, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => Halo.Scale = v, 1.08, 1, Easing.SinInOut) },
        };
        breathe.Commit(Halo, "breathe", length: 1800, repeat: () => true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Halo.AbortAnimation("breathe");
    }
}

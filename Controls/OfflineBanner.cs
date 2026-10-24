using BusTicket.Services;

namespace BusTicket.Controls;

/// <summary>Bandeau orange « Mode hors ligne », visible tant que le serveur est injoignable.</summary>
public class OfflineBanner : ContentView
{
    private readonly AlertBanner _banner = new() { Kind = AlertKind.Warning };
    private Backend? _backend;

    public OfflineBanner()
    {
        Content = _banner;
        IsVisible = false;
        Loaded += (_, _) =>
        {
            _backend ??= IPlatformApplication.Current?.Services.GetService<Backend>();
            if (_backend is null) return;
            _backend.StatusChanged += OnStatusChanged;
            Refresh();
        };
        Unloaded += (_, _) => { if (_backend is not null) _backend.StatusChanged -= OnStatusChanged; };
    }

    private void OnStatusChanged(object? sender, EventArgs e) => Refresh();

    private async void Refresh()
    {
        var offline = _backend?.IsOffline == true;
        _banner.Message = offline
            ? "Mode hors ligne : serveur injoignable. Vos réservations sont enregistrées sur ce téléphone."
            : null;
        if (offline == IsVisible) return;
        if (offline)
        {
            Opacity = 0;
            IsVisible = true;
            await this.FadeToAsync(1, 250);
        }
        else
        {
            IsVisible = false;
        }
    }
}

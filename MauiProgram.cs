using BusTicket.Services;
using BusTicket.ViewModels;
using BusTicket.Views.Auth;
using BusTicket.Views.Booking;
using BusTicket.Views.Profile;
using BusTicket.Views.Search;
using BusTicket.Views.Tickets;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace BusTicket;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Roboto-Regular.ttf", "Roboto");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                fonts.AddFont("MaterialIcons-Regular.otf", "MaterialIcons");
            });

        RegisterServices(builder.Services);
        RemoveNativeFieldBorders();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    /// <summary>Injection de dépendances : services partagés, puis un couple ViewModel + Page par écran.</summary>
    private static void RegisterServices(IServiceCollection services)
    {
        // Services (une seule instance pour toute l'application)
        services.AddSingleton<AuthService>();
        services.AddSingleton<TripService>();
        services.AddSingleton<BookingRepository>();
        services.AddSingleton<SearchHistory>();
        services.AddSingleton<Navigator>();

        // Écrans
        services.AddTransient<LoginViewModel>().AddTransient<LoginPage>();
        services.AddTransient<RegisterViewModel>().AddTransient<RegisterPage>();
        services.AddTransient<SearchViewModel>().AddTransient<SearchPage>();
        services.AddTransient<ResultsViewModel>().AddTransient<ResultsPage>();
        services.AddTransient<SeatSelectionViewModel>().AddTransient<SeatSelectionPage>();
        services.AddTransient<PaymentViewModel>().AddTransient<PaymentPage>();
        services.AddTransient<MyTicketsViewModel>().AddTransient<MyTicketsPage>();
        services.AddTransient<TicketViewModel>().AddTransient<TicketPage>();
        services.AddTransient<ProfileViewModel>().AddTransient<ProfilePage>();
    }

    /// <summary>
    /// Les champs natifs ont leur propre soulignement / bordure : on les retire,
    /// la bordure est dessinée par nos contrôles (AppTextField, AppDateField).
    /// </summary>
    private static void RemoveNativeFieldBorders()
    {
        EntryHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background = null;
            handler.PlatformView.Resources["TextControlBackgroundFocused"] =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
        });

        DatePickerHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
        });
    }
}

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.ViewModels;
using RoclandAccesoControl.Mobile.Views;

namespace RoclandAccesoControl.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // ── Servicios ──────────────────────────────────────────────────
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddSingleton<AuthStateService>();

        // ── ViewModels ─────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<SolicitudesViewModel>();
        builder.Services.AddTransient<DetalleSolicitudViewModel>();
        builder.Services.AddTransient<AccesosActivosViewModel>();

        // ── Pages ──────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<SolicitudesPage>();
        builder.Services.AddTransient<DetalleSolicitudPage>();
        builder.Services.AddTransient<AccesosActivosPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
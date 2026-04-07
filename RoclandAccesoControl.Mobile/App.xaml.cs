using RoclandAccesoControl.Mobile.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace RoclandAccesoControl.Mobile;

public partial class App : Application
{
    private readonly AuthStateService _auth;

    public App(AuthStateService auth)
    {
        InitializeComponent();
        _auth = auth;

        MainPage = new AppShell();

        // Suscribirse al evento de "Notificación Tocada"
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
    }

    // Método que se ejecuta cuando el usuario toca la notificación
    private void OnNotificationTapped(NotificationActionEventArgs e)
    {
        if (e.IsTapped && !string.IsNullOrEmpty(e.Request.ReturningData))
        {
            // Recuperamos el ID que guardamos en MyFirebaseMessagingService o SignalR
            string solicitudId = e.Request.ReturningData;

            // Navegamos usando el hilo principal para evitar bloqueos visuales
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Como ya tienes registrada la ruta DetalleSolicitudPage en tu AppShell...
                await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={solicitudId}");
            });
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            var sesionRestaurada = await _auth.RestaurarSesionAsync();
            await Shell.Current.GoToAsync(sesionRestaurada ? "//Solicitudes" : "//Login");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Error de inicio",
                $"{ex.GetType().Name}\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                "OK");
        }
    }
}
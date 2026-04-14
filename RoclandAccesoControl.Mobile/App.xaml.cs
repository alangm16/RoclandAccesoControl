using RoclandAccesoControl.Mobile.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace RoclandAccesoControl.Mobile;

public partial class App : Application
{
    private readonly AuthStateService _auth;
    private string? _idNotificacionPendiente = null;

    public App(AuthStateService auth)
    {
        InitializeComponent();
        _auth = auth;

        MainPage = new AppShell();

        // Suscripción al evento
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
    }

    private void OnNotificationTapped(NotificationActionEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Espera a que la UI esté lista (especialmente tras un arranque en frío)
            await Task.Delay(300);

            string data = e.Request?.ReturningData ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"[NOTIF TAP] ReturningData = '{data}'");

            if (string.IsNullOrEmpty(data))
            {
                // Opcional: mostrar alerta solo para depuración
                 await App.Current.MainPage.DisplayAlert("Sin datos", "No se recibió ID", "OK");
                return;
            }

            try
            {
                await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={data}");
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error Navegación", ex.Message, "OK");
            }
        });
    }
    protected override async void OnStart()
    {
        base.OnStart();

        bool sesionRestaurada = false;
        try
        {
            sesionRestaurada = await _auth.RestaurarSesionAsync();
            await Shell.Current.GoToAsync(sesionRestaurada ? "//Bitacora" : "//Login");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            return;
        }

        // 👉 FIX 3: Procesar la pendiente solo si la sesión se restauró con éxito
        if (sesionRestaurada && !string.IsNullOrEmpty(_idNotificacionPendiente))
        {
            var id = _idNotificacionPendiente;
            _idNotificacionPendiente = null;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500); // Esperar a que renderice Bitácora
                await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={id}");
            });
        }
        else
        {
            _idNotificacionPendiente = null;
        }
    }
}
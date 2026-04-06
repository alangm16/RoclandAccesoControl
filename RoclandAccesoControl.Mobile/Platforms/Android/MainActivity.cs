using Android.App;
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.OS;
using Android.Util;
using Firebase;
using Firebase.Messaging;
using System.Threading.Tasks;

namespace RoclandAccesoControl.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
    ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
    ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        FirebaseApp.InitializeApp(this);

        var app = FirebaseApp.InitializeApp(this);
        if (app == null)
            Log.Error("FCM", "Firebase no se inicializó correctamente");
        else
            Log.Debug("FCM", "Firebase inicializado correctamente");

            _ = GetFcmTokenAsync();
         CrearCanalNotificaciones();
        SolicitarPermisoNotificaciones();
    }

    private async System.Threading.Tasks.Task GetFcmTokenAsync()
    {
        try
        {
            var androidTask = FirebaseMessaging.Instance.GetToken();
            string token = await androidTask.ToSystemTask(); // usa el helper estático
            Android.Util.Log.Debug("FCM", $"Token obtenido: {token}");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("FCM", $"Error al obtener token: {ex.Message}");
        }
    }

    private void CrearCanalNotificaciones()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var canal = new NotificationChannel(
            "acceso_control",
            "Control de Acceso",
            NotificationImportance.High)
        {
            Description = "Notificaciones de solicitudes de acceso"
        };
        canal.EnableVibration(true);
        canal.EnableLights(true);

        var manager = GetSystemService(NotificationService) as NotificationManager;
        manager?.CreateNotificationChannel(canal);
    }

    private void SolicitarPermisoNotificaciones()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
        {
            RequestPermissions(
                new[] { Android.Manifest.Permission.PostNotifications }, 101);
        }
    }

    public override void OnRequestPermissionsResult(
        int requestCode, string[] permissions, Permission[] grantResults)
    {
        if (requestCode == 101
            && grantResults.Length > 0
            && grantResults[0] == Permission.Granted)
        {
            System.Diagnostics.Debug.WriteLine("[FCM] Permiso de notificaciones concedido.");
        }
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
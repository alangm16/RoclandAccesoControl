using Android.App;
using Android.Content.PM;
using Android.OS;
using Firebase;

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
        CrearCanalNotificaciones();
        SolicitarPermisoNotificaciones();
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
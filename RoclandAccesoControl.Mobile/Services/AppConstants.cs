namespace RoclandAccesoControl.Mobile.Services;

public static class AppConstants
{
    // ── Cambiar estas URLs en appsettings.Secrets.json antes de compilar ──
    // Android usa 10.0.2.2 para acceder a localhost del host desde el emulador
    //public const string BaseUrlAndroid = "https://10.0.2.2:7000";
    // Para dispositivo físico Android en la misma red, usa la IP de tu máquina
    public const string BaseUrlAndroid = "https://192.168.1.24:7000";
    public const string BaseUrlWindows = "https://localhost:7000";
    public const string SignalRHubPath = "/accesohub";
}
using Microsoft.AspNetCore.SignalR.Client;
using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly AuthStateService _auth;

    // Eventos que la UI puede suscribir
    public event Action<NuevaSolicitudEvent>? NuevaSolicitudRecibida;
    public event Action<int, string>? SolicitudResuelta; // (solicitudId, estado)
    public event Action<HubConnectionState>? EstadoConexionCambiado;

    public HubConnectionState Estado =>
        _connection?.State ?? HubConnectionState.Disconnected;

    public SignalRService(AuthStateService auth)
    {
        _auth = auth;
    }

    public async Task ConectarAsync()
    {
        if (_connection?.State == HubConnectionState.Connected) return;

        var hubUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? AppConstants.BaseUrlAndroid + AppConstants.SignalRHubPath
            : AppConstants.BaseUrlWindows + AppConstants.SignalRHubPath;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // JWT en query string para SignalR
                options.AccessTokenProvider = () =>
                    Task.FromResult<string?>(_auth.Token);

                // Aceptar certificados autofirmados en desarrollo
                options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            })
            .WithAutomaticReconnect(new[] {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        // ── Eventos del hub ────────────────────────────────────────────
        _connection.On<NuevaSolicitudEvent>("NuevaSolicitud", solicitud =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                NuevaSolicitudRecibida?.Invoke(solicitud));
        });

        _connection.On<object>("SolicitudResuelta", data =>
        {
            // Notificar que la lista puede haber cambiado
            MainThread.BeginInvokeOnMainThread(() =>
                SolicitudResuelta?.Invoke(0, "Actualizado"));
        });

        // ── Estado de conexión ─────────────────────────────────────────
        _connection.Reconnecting += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Reconnecting));
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Connected));
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Disconnected));
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
        EstadoConexionCambiado?.Invoke(HubConnectionState.Connected);
    }

    public async Task DesconectarAsync()
    {
        if (_connection is not null)
            await _connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
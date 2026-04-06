using Microsoft.AspNetCore.SignalR.Client;
using RoclandAccesoControl.Mobile.Models;
using System.Text.Json;

namespace RoclandAccesoControl.Mobile.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly AuthStateService _auth;

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
                options.AccessTokenProvider = () =>
                    Task.FromResult<string?>(_auth.Token);

                // Aceptar certificados autofirmados en desarrollo
                options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        // ── NuevaSolicitud ─────────────────────────────────────────────
        _connection.On<NuevaSolicitudEvent>("NuevaSolicitud", solicitud =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                NuevaSolicitudRecibida?.Invoke(solicitud));
        });

        // ── SolicitudResuelta ──────────────────────────────────────────
        // El servidor puede enviar un objeto JSON con SolicitudId y Estado.
        // Deserializamos manualmente para extraer el id real y notificar la UI.
        _connection.On<JsonElement>("SolicitudResuelta", data =>
        {
            int id = 0;
            string estado = "Resuelta";

            try
            {
                // El servidor puede enviar { solicitudId: 5, estado: "Aprobada" }
                // Intentamos con variantes de capitalización
                if (data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("solicitudId", out var idProp)
                        || data.TryGetProperty("SolicitudId", out idProp))
                        id = idProp.GetInt32();

                    if (data.TryGetProperty("estado", out var estadoProp)
                        || data.TryGetProperty("Estado", out estadoProp))
                        estado = estadoProp.GetString() ?? estado;
                }
                else if (data.ValueKind == JsonValueKind.Number)
                {
                    // El servidor envía el id directamente como número
                    id = data.GetInt32();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SignalR] Error parseando SolicitudResuelta: {ex.Message}");
            }

            MainThread.BeginInvokeOnMainThread(() =>
                SolicitudResuelta?.Invoke(id, estado));
        });

        // ── Estado de conexión ─────────────────────────────────────────
        _connection.Reconnecting += error =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                EstadoConexionCambiado?.Invoke(HubConnectionState.Reconnecting));
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
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
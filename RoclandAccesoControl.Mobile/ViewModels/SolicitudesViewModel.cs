using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;
using System.Collections.ObjectModel;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class SolicitudesViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private readonly AuthStateService _auth;

    [ObservableProperty] private ObservableCollection<SolicitudPendiente> _solicitudes = [];
    [ObservableProperty] private string _estadoConexion = "Conectando...";
    [ObservableProperty] private Color _colorEstadoConexion = Colors.Orange;
    [ObservableProperty] private int _cantidadPendientes;
    [ObservableProperty] private bool _sinSolicitudes = true;

    public SolicitudesViewModel(ApiService api, SignalRService signalR, AuthStateService auth)
    {
        _api = api;
        _signalR = signalR;
        _auth = auth;
        Titulo = "Solicitudes";

        _signalR.NuevaSolicitudRecibida += OnNuevaSolicitud;
        _signalR.EstadoConexionCambiado += OnEstadoCambiado;
    }

    [RelayCommand]
    public async Task InicializarAsync()
    {
        await CargarSolicitudesAsync();
        await ConectarSignalRAsync();
    }

    [RelayCommand]
    public async Task CargarSolicitudesAsync()
    {
        EstaCargando = true;
        try
        {
            var lista = await _api.ObtenerSolicitudesAsync();
            Solicitudes = new ObservableCollection<SolicitudPendiente>(lista);
            CantidadPendientes = lista.Count;
            SinSolicitudes = lista.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar las solicitudes: {ex.Message}", "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task VerDetallAsync(SolicitudPendiente solicitud)
    {
        if (solicitud is null) return;
        await Shell.Current.GoToAsync(nameof(DetalleSolicitudPage),
            new Dictionary<string, object> { { "Solicitud", solicitud } });
    }

    [RelayCommand]
    private async Task IrAActivosAsync()
    {
        await Shell.Current.GoToAsync("//AccesosActivos");
    }

    [RelayCommand]
    private void CerrarSesion()
    {
        _auth.CerrarSesion();
        Shell.Current.GoToAsync("//Login");
    }

    private async Task ConectarSignalRAsync()
    {
        try
        {
            await _signalR.ConectarAsync();
        }
        catch (Exception ex)
        {
            EstadoConexion = "Sin conexión";
            ColorEstadoConexion = Colors.Red;
            await Shell.Current.DisplayAlert("SignalR", $"No se pudo conectar: {ex.Message}", "OK");
        }
    }

    private void OnNuevaSolicitud(NuevaSolicitudEvent evento)
    {
        var solicitud = new SolicitudPendiente
        {
            SolicitudId = evento.SolicitudId,
            RegistroId = evento.RegistroId,
            TipoRegistro = evento.TipoRegistro,
            NombrePersona = evento.NombrePersona,
            Empresa = evento.Empresa,
            NumeroIdentificacion = evento.NumeroIdentificacion,
            TipoID = evento.TipoID,
            Motivo = evento.Motivo,
            Area = evento.Area,
            FechaSolicitud = evento.FechaSolicitud
        };

        Solicitudes.Insert(0, solicitud);
        CantidadPendientes = Solicitudes.Count;
        SinSolicitudes = false;

        // Notificación local
        EnviarNotificacionLocal(solicitud);
    }

    private void OnEstadoCambiado(HubConnectionState estado)
    {
        (EstadoConexion, ColorEstadoConexion) = estado switch
        {
            HubConnectionState.Connected => ("● Conectado", Color.FromArgb("#22C55E")),
            HubConnectionState.Reconnecting => ("◌ Reconectando...", Colors.Orange),
            _ => ("✕ Desconectado", Colors.Red)
        };
    }

    private static void EnviarNotificacionLocal(SolicitudPendiente s)
    {
        var notification = new NotificationRequest
        {
            NotificationId = s.SolicitudId,
            Title = $"Nueva solicitud — {s.TipoRegistro}",
            Description = $"{s.NombrePersona} · {s.Motivo}",
            BadgeNumber = 1,
            CategoryType = NotificationCategoryType.Status,
            Android =
        {
            ChannelId = "acceso_control",
            Priority = AndroidPriority.High,
            IsGroupSummary = false
        }
        };

        LocalNotificationCenter.Current.Show(notification);
    }
}
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Models.Entities;

namespace RoclandAccesoControl.Web.Services.Interfaces;

public interface IAccesoService
{
    Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numId);
    Task<VisitanteResponse> RegistrarVisitanteAsync(CrearVisitanteRequest req, string ip);
    Task<ProveedorResponse> RegistrarProveedorAsync(CrearProveedorRequest req, string ip);
    Task<IEnumerable<SolicitudPendienteResponse>> ObtenerSolicitudesPendientesAsync();
    Task<SolicitudPendienteResponse?> ObtenerSolicitudPorIdAsync(int solicitudId);
    Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosAsync();
    Task<bool> AprobarSolicitudAsync(AprobarSolicitudRequest request);
    Task<bool> RechazarSolicitudAsync(RechazarSolicitudRequest request);
    Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request);
    Task<bool> GuardarFcmTokenAsync(int guardiaId, string fcmToken);
    Task<IEnumerable<GafeteDisponibleResponse>> ObtenerGafetesDisponiblesAsync();
}

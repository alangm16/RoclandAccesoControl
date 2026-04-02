using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Services.Interfaces;

public interface IAccesoService
{
    Task<VisitanteResponse> RegistrarVisitanteAsync(CrearVisitanteRequest request, string ipSolicitud);
    Task<ProveedorResponse> RegistrarProveedorAsync(CrearProveedorRequest request, string ipSolicitud);
    Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numeroId);
    Task<bool> AprobarSolicitudAsync(AprobarSolicitudRequest request);
    Task<bool> RechazarSolicitudAsync(RechazarSolicitudRequest request);
    Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request);
    Task<IEnumerable<SolicitudPendienteResponse>> ObtenerSolicitudesPendientesAsync();
    Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosAsync();
}

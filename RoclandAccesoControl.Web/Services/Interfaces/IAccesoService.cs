using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Services.Interfaces;

public interface IAccesoService
{
    Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numId);
    Task<VisitanteResponse> RegistrarVisitanteAsync(CrearVisitanteRequest req, string ip);
    Task<ProveedorResponse> RegistrarProveedorAsync(CrearProveedorRequest req, string ip);
    Task<IEnumerable<SolicitudPendienteResponse>> ObtenerSolicitudesPendientesAsync();
    Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosAsync();
    Task<bool> AprobarSolicitudAsync(AprobarSolicitudRequest request);
    Task<bool> RechazarSolicitudAsync(RechazarSolicitudRequest request);
    Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request);
}

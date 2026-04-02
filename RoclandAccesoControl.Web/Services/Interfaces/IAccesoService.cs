using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Services.Interfaces;

public interface IAccesoService
{
    Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numId);
    Task<VisitanteResponse> RegistrarVisitanteAsync(CrearVisitanteRequest req, string ip);
    Task<ProveedorResponse> RegistrarProveedorAsync(CrearProveedorRequest req, string ip);
}

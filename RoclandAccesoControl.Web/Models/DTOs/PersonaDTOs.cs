namespace RoclandAccesoControl.Web.Models.DTOs;

public record PersonaBusquedaResponse(
    int Id,
    string Nombre,
    string TipoID,
    string NumeroIdentificacion,
    string? Empresa,
    string? Telefono,
    string? Email,
    int TotalVisitas,
    DateTime? FechaUltimaVisita
);

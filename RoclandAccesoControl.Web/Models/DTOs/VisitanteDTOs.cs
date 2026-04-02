namespace RoclandAccesoControl.Web.Models.DTOs;

public record CrearVisitanteRequest(
    // Datos de persona
    string Nombre,
    int TipoIdentificacionId,
    string NumeroIdentificacion,
    string? Telefono,
    string? Email,
    // Datos de visita
    int AreaId,
    int MotivoId,
    bool ConsentimientoFirmado,
    string? Observaciones
);

public record VisitanteResponse(
    int RegistroId,
    int PersonaId,
    string Nombre,
    string Area,
    string Motivo,
    string EstadoAcceso,
    DateTime FechaEntrada
);
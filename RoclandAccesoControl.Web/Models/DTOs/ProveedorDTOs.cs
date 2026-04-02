namespace RoclandAccesoControl.Web.Models.DTOs;

public record CrearProveedorRequest(
    // Datos de persona
    string Nombre,
    int TipoIdentificacionId,
    string NumeroIdentificacion,
    string Empresa,
    string? Telefono,
    string? Email,
    // Datos de visita
    int MotivoId,
    string? UnidadPlacas,
    string? FacturaRemision,
    bool ConsentimientoFirmado,
    string? Observaciones
);

public record ProveedorResponse(
    int RegistroId,
    int PersonaId,
    string Nombre,
    string Empresa,
    string Motivo,
    string EstadoAcceso,
    DateTime FechaEntrada
);
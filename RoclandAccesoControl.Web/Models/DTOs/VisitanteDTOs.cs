using System.ComponentModel.DataAnnotations;

namespace RoclandAccesoControl.Web.Models.DTOs;

public record CrearVisitanteRequest(
    [Required, StringLength(150, MinimumLength = 2)] string Nombre,
    [Range(1, int.MaxValue)] int TipoIdentificacionId,
    [Required, StringLength(60, MinimumLength = 3)] string NumeroIdentificacion,
    [StringLength(20)] string? Telefono,
    [EmailAddress, StringLength(100)] string? Email,
    [Range(1, int.MaxValue)] int AreaId,
    [Range(1, int.MaxValue)] int MotivoId,
    bool ConsentimientoFirmado,
    [StringLength(500)] string? Observaciones
);

public record VisitanteResponse(
    int RegistroId,
    int PersonaId,
    string Nombre,
    string Area,
    string Motivo,
    string EstadoAcceso,
    DateTime FechaEntrada,
    bool EsRecurrente,
    int TotalVisitasPrevias
);
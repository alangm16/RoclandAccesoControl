using System.ComponentModel.DataAnnotations;

namespace RoclandAccesoControl.Web.Models.DTOs;

public record CrearProveedorRequest(
    [Required, StringLength(150, MinimumLength = 2)] string Nombre,
    [Range(1, int.MaxValue)] int TipoIdentificacionId,
    [Required, StringLength(60, MinimumLength = 3)] string NumeroIdentificacion,
    [Required, StringLength(150, MinimumLength = 2)] string Empresa,
    [StringLength(20)] string? Telefono,
    [EmailAddress, StringLength(100)] string? Email,
    [Range(1, int.MaxValue)] int MotivoId,
    [StringLength(30)] string? UnidadPlacas,
    [StringLength(100)] string? FacturaRemision,
    bool ConsentimientoFirmado,
    [StringLength(500)] string? Observaciones
);

public record ProveedorResponse(
    int RegistroId,
    int PersonaId,
    string Nombre,
    string Empresa,
    string Motivo,
    string EstadoAcceso,
    DateTime FechaEntrada,
    bool EsRecurrente,
    int TotalVisitasPrevias
);
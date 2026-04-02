using System.ComponentModel.DataAnnotations;

namespace RoclandAccesoControl.Web.Models.DTOs;

// ── Request: crear registro de visitante ──────────────────────────────
public class CrearVisitanteRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de identificación es obligatorio.")]
    public int TipoIdentificacionId { get; set; }

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "El número de ID debe tener entre 3 y 60 caracteres.")]
    public string NumeroIdentificacion { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Empresa { get; set; }

    [Phone(ErrorMessage = "Teléfono inválido.")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "El área de visita es obligatoria.")]
    public int AreaId { get; set; }

    [Required(ErrorMessage = "El motivo de visita es obligatorio.")]
    public int MotivoId { get; set; }

    [Required(ErrorMessage = "Debe aceptar el consentimiento.")]
    public bool ConsentimientoFirmado { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

// ── Response ──────────────────────────────────────────────────────────
public class VisitanteResponse
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string EstadoAcceso { get; set; } = string.Empty;
    public DateTime FechaEntrada { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

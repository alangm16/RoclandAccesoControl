namespace RoclandAccesoControl.Web.Models.DTOs;

// ── Response del autocompletado ───────────────────────────────────────
public class PersonaBusquedaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoId { get; set; } = string.Empty;
    public int TipoIdentificacionId { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int TotalVisitas { get; set; }
    public DateTime? FechaUltimaVisita { get; set; }
}

// ── Catálogos para los dropdowns ──────────────────────────────────────
public class CatalogoItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

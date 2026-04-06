namespace RoclandAccesoControl.Web.Models.Entities
{
    public class Persona
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int TipoIdentificacionId { get; set; }
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string? Empresa { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? FechaUltimaVisita { get; set; }
        public int TotalVisitas { get; set; } = 0;
        public bool Activo { get; set; } = true;

        public TipoIdentificacion TipoIdentificacion { get; set; } = null!;
        public ICollection<RegistroVisitante> RegistrosVisitantes { get; set; } = [];
        public ICollection<RegistroProveedor> RegistrosProveedores { get; set; } = [];
        public ICollection<SolicitudPendiente> SolicitudesPendientes { get; set; } = [];
    }
}

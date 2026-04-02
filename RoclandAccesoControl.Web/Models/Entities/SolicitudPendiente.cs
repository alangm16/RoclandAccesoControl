namespace RoclandAccesoControl.Web.Models.Entities
{
    public class SolicitudPendiente
    {
        public int Id { get; set; }
        public string TipoRegistro { get; set; } = string.Empty; // "Visitante" | "Proveedor"
        public int RegistroId { get; set; }
        public int PersonaId { get; set; }
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public string Estado { get; set; } = "Pendiente";
        public int? GuardiaId { get; set; }

        public Persona Persona { get; set; } = null!;
        public Guardia? Guardia { get; set; }
    }
}

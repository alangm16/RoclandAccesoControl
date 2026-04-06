namespace RoclandAccesoControl.Web.Models.Entities
{
    public class Guardia
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? DeviceToken { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<RegistroVisitante> EntradasAutorizadas { get; set; } = [];
        public ICollection<RegistroVisitante> SalidasAutorizadas { get; set; } = [];
        public ICollection<RegistroProveedor> EntradasProvAutorizadas { get; set; } = [];
        public ICollection<RegistroProveedor> SalidasProvAutorizadas { get; set; } = [];
        public ICollection<SolicitudPendiente> SolicitudesAtendidas { get; set; } = [];
    }
}

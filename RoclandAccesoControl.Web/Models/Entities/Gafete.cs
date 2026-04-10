namespace RoclandAccesoControl.Web.Models.Entities;

public class Gafete
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = "Libre"; // Libre, En uso, Bloqueado
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // Relaciones inversas (opcional)
    public ICollection<RegistroVisitante> RegistrosVisitantes { get; set; } = [];
    public ICollection<RegistroProveedor> RegistrosProveedores { get; set; } = [];
}
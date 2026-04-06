namespace RoclandAccesoControl.Web.Models.Entities
{
    public class MotivoVisita
    {
       public int Id { get; set; }
       public string Nombre { get; set; } = string.Empty;
       public bool Activo { get; set; } = true;

       public ICollection<RegistroVisitante> RegistrosVisitantes { get; set; } = [];
       public ICollection<RegistroProveedor> RegistroProveedores { get; set; } = [];
        
    }
}

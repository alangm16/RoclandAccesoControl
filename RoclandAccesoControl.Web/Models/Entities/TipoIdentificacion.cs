namespace RoclandAccesoControl.Web.Models.Entities
{
    public class TipoIdentificacion
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public ICollection<Persona> Personas { get; set; } = [];
    }
}

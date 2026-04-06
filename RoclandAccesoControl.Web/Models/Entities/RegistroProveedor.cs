namespace RoclandAccesoControl.Web.Models.Entities
{
    public class RegistroProveedor
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public int MotivoId { get; set; }
        public DateTime FechaEntrada { get; set; }
        public DateTime? FechaSalida { get; set; }
        public string? UnidadPlacas { get; set; }
        public string? FacturaRemision { get; set; }
        public string? NumeroGafete { get; set; }
        public int GuardiaEntradaId { get; set; }
        public int? GuardiaSalidaId { get; set; }
        public string EstadoAcceso { get; set; } = "Pendiente";
        public bool ConsentimientoFirmado { get; set; } = false;
        public string? Observaciones { get; set; }
        public string? IPSolicitud { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public TimeOnly? HoraEntrada { get; set; }
        public TimeOnly? HoraSalida { get; set; }
        public int? MinutosEstancia { get; set; }

        public Persona Persona { get; set; } = null!;
        public MotivoVisita Motivo { get; set; } = null!;
        public Guardia GuardiaEntrada { get; set; } = null!;
        public Guardia? GuardiaSalida { get; set; }
    }
}

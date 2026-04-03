namespace RoclandAccesoControl.Mobile.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime Expiracion { get; set; }
}

public class SolicitudPendiente
{
    public int SolicitudId { get; set; }
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public int PersonaId { get; set; }
    public string NombrePersona { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string TipoID { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Area { get; set; }
    public DateTime FechaSolicitud { get; set; }

    // Computed para la UI
    public string TipoIcono => TipoRegistro == "Visitante" ? "👤" : "🚚";
    public string TipoColor => TipoRegistro == "Visitante" ? "#2563EB" : "#7C3AED";
    public string HoraFormateada => FechaSolicitud.ToLocalTime().ToString("HH:mm");
    public string AreaOEmpresa => TipoRegistro == "Visitante" ? (Area ?? "") : (Empresa ?? "");
}

public class AccesoActivo
{
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public string NombrePersona { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string NumeroGafete { get; set; } = string.Empty;
    public DateTime FechaEntrada { get; set; }
    public string Area { get; set; } = string.Empty;

    public string TipoIcono => TipoRegistro == "Visitante" ? "👤" : "🚚";
    public string HoraEntradaFormateada => FechaEntrada.ToLocalTime().ToString("HH:mm");
    public string TiempoTranscurrido
    {
        get
        {
            var diff = DateTime.UtcNow - FechaEntrada;
            if (diff.TotalHours >= 1)
                return $"{(int)diff.TotalHours}h {diff.Minutes}m";
            return $"{diff.Minutes}m";
        }
    }
}

public class AprobarRequest
{
    public int SolicitudId { get; set; }
    public int GuardiaId { get; set; }
    public string NumeroGafete { get; set; } = string.Empty;
}

public class RechazarRequest
{
    public int SolicitudId { get; set; }
    public int GuardiaId { get; set; }
    public string? Motivo { get; set; }
}

public class MarcarSalidaRequest
{
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public int GuardiaId { get; set; }
}

// Evento que llega por SignalR
public class NuevaSolicitudEvent
{
    public int SolicitudId { get; set; }
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public string NombrePersona { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string TipoID { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Area { get; set; }
    public DateTime FechaSolicitud { get; set; }
}
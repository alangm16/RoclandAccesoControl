namespace RoclandAccesoControl.Web.Models.DTOs;

public record NuevaSolicitudEvent(
    int SolicitudId,
    int RegistroId,
    string TipoRegistro,
    string NombrePersona,
    string? Empresa,
    string NumeroIdentificacion,
    string TipoID,
    string Motivo,
    string? Area,
    DateTime FechaSolicitud
);

public record SolicitudResueltaEvent(
    int SolicitudId,
    string Estado,         // "Aprobado" | "Rechazado"
    string NombreGuardia
);
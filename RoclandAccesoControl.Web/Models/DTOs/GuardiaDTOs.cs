namespace RoclandAccesoControl.Web.Models.DTOs;

public record AprobarSolicitudRequest(
    int SolicitudId,
    int GuardiaId,
    string NumeroGafete
);

public record RechazarSolicitudRequest(
    int SolicitudId,
    int GuardiaId,
    string? Motivo
);

public record MarcarSalidaRequest(
    int RegistroId,
    string TipoRegistro,   // "Visitante" | "Proveedor"
    int GuardiaId
);

public record SolicitudPendienteResponse(
    int SolicitudId,
    int RegistroId,
    string TipoRegistro,
    int PersonaId,
    string NombrePersona,
    string? Empresa,
    string NumeroIdentificacion,
    string TipoID,
    string Motivo,
    string? Area,
    DateTime FechaSolicitud
);

public record AccesoActivoResponse(
    int RegistroId,
    string TipoRegistro,
    string NombrePersona,
    string? Empresa,
    string NumeroGafete,
    DateTime FechaEntrada,
    string Area
);
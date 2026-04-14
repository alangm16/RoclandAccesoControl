namespace RoclandAccesoControl.Web.Models.DTOs;

public record GafeteDisponibleResponse(
    int Id,
    string Codigo
);

public record AprobarSolicitudRequest(
    int SolicitudId,
    int GuardiaId,
    int GafeteId
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
    DateTime FechaSolicitud,
    string? Placas
);

public record AccesoActivoResponse(
    int RegistroId,
    string TipoRegistro,
    string NombrePersona,
    string? Empresa,
    string NumeroGafete,        // se llenará con gafete.Codigo o ""
    DateTime FechaEntrada,
    string Area,
    double MinutosLlevaDentro
);

public record RegistrarFcmTokenRequest(
    int GuardiaId,
    string FcmToken
);
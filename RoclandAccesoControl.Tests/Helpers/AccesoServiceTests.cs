using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using RoclandAccesoControl.Tests.Helpers;
using RoclandAccesoControl.Web.Hubs;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services;

namespace RoclandAccesoControl.Tests.Services;

public class AccesoServiceTests
{
    private AccesoService CrearServicio(string dbNombre)
    {
        var ctx = DbContextHelper.CrearContexto(dbNombre);

        var hubClients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        hubClients.Setup(h => h.Group(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<AccesoHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutoCerrarSalidaHoras"] = "24"
            })
            .Build();

        return new AccesoService(ctx, hubContext.Object, config);
    }

    // ── US-14: Timestamps del servidor ─────────────────────────────────
    [Fact]
    public async Task RegistrarVisitante_FechaEntradaEsDelServidor()
    {
        var svc = CrearServicio("TestTimestamp");
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var request = new CrearVisitanteRequest(
            "Juan Pérez", 1, "INE123456", null, null,
            1, 1, true, null);

        var result = await svc.RegistrarVisitanteAsync(request, "127.0.0.1");

        result.FechaEntrada.Should().BeAfter(antes);
        result.FechaEntrada.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    // ── US-01: Registro de visitante ───────────────────────────────────
    [Fact]
    public async Task RegistrarVisitante_CreaRegistroYPersona()
    {
        var svc = CrearServicio("TestRegistroVis");

        var request = new CrearVisitanteRequest(
            "María López", 1, "INE789012", "5551234567",
            "maria@test.com", 1, 1, true, null);

        var result = await svc.RegistrarVisitanteAsync(request, "10.0.0.1");

        result.Should().NotBeNull();
        result.Nombre.Should().Be("María López");
        result.EstadoAcceso.Should().Be("Pendiente");
        result.RegistroId.Should().BeGreaterThan(0);
        result.PersonaId.Should().BeGreaterThan(0);
    }

    // ── US-02: Registro de proveedor ───────────────────────────────────
    [Fact]
    public async Task RegistrarProveedor_CreaRegistroConEmpresa()
    {
        var svc = CrearServicio("TestRegistroProv");

        var request = new CrearProveedorRequest(
            "Carlos Ruiz", 1, "INE345678",
            "Distribuidora XYZ", "5559876543", null,
            2, "XYZ-123", "FAC-456", true, "Entrega urgente");

        var result = await svc.RegistrarProveedorAsync(request, "10.0.0.2");

        result.Should().NotBeNull();
        result.Empresa.Should().Be("Distribuidora XYZ");
        result.EstadoAcceso.Should().Be("Pendiente");
    }

    // ── US-03: Autocompletado por ID ───────────────────────────────────
    [Fact]
    public async Task BuscarPersona_RetornaPersonaExistente()
    {
        var svc = CrearServicio("TestBusqueda");

        // Registrar primero para que exista la persona
        await svc.RegistrarVisitanteAsync(
            new CrearVisitanteRequest("Ana García", 1, "AUTOTEST001",
                null, null, 1, 1, true, null), "127.0.0.1");

        var result = await svc.BuscarPersonaAsync("AUTOTEST001");

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Ana García");
        result.NumeroIdentificacion.Should().Be("AUTOTEST001");
    }

    [Fact]
    public async Task BuscarPersona_RetornaNull_CuandoNoExiste()
    {
        var svc = CrearServicio("TestBusquedaVacia");
        var result = await svc.BuscarPersonaAsync("INEXISTENTE999");
        result.Should().BeNull();
    }

    // ── Perfil recurrente ──────────────────────────────────────────────
    [Fact]
    public async Task RegistrarVisitante_PersonaRecurrente_IncrementaContador()
    {
        var svc = CrearServicio("TestRecurrente");
        var request = new CrearVisitanteRequest(
            "Luis Torres", 1, "RECURRENTE001",
            null, null, 1, 1, true, null);

        // Primera visita
        var r1 = await svc.RegistrarVisitanteAsync(request, "127.0.0.1");
        r1.EsRecurrente.Should().BeFalse();
        r1.TotalVisitasPrevias.Should().Be(0);

        // Segunda visita — misma persona
        var r2 = await svc.RegistrarVisitanteAsync(request, "127.0.0.1");
        r2.EsRecurrente.Should().BeTrue();
        r2.TotalVisitasPrevias.Should().Be(0); // aún no aprobada la primera
    }

    // ── US-05: Aprobar solicitud ───────────────────────────────────────
    [Fact]
    public async Task AprobarSolicitud_ActualizaEstadoYGafete()
    {
        var svc = CrearServicio("TestAprobar");
        var reg = await svc.RegistrarVisitanteAsync(
            new CrearVisitanteRequest("Pedro Solis", 1, "APRUEBA001",
                null, null, 1, 1, true, null), "127.0.0.1");

        // Obtener la solicitud creada
        var solicitudes = await svc.ObtenerSolicitudesPendientesAsync();
        var solicitud = solicitudes.First();

        var ok = await svc.AprobarSolicitudAsync(new AprobarSolicitudRequest(
            solicitud.SolicitudId, 1, "G-042"));

        ok.Should().BeTrue();

        var activos = await svc.ObtenerAccesosActivosAsync();
        activos.Should().ContainSingle(a =>
            a.NombrePersona == "Pedro Solis" &&
            a.NumeroGafete == "G-042");
    }

    // ── US-06: Marcar salida ───────────────────────────────────────────
    [Fact]
    public async Task MarcarSalida_ActualizaFechaSalida()
    {
        var svc = CrearServicio("TestSalida");
        await svc.RegistrarVisitanteAsync(
            new CrearVisitanteRequest("Rosa Méndez", 1, "SALIDA001",
                null, null, 1, 1, true, null), "127.0.0.1");

        var solicitudes = await svc.ObtenerSolicitudesPendientesAsync();
        await svc.AprobarSolicitudAsync(
            new AprobarSolicitudRequest(solicitudes.First().SolicitudId, 1, "G-007"));

        var activos = await svc.ObtenerAccesosActivosAsync();
        var activo = activos.First();

        var ok = await svc.MarcarSalidaAsync(
            new MarcarSalidaRequest(activo.RegistroId, "Visitante", 1));

        ok.Should().BeTrue();

        var activosPostSalida = await svc.ObtenerAccesosActivosAsync();
        activosPostSalida.Should().BeEmpty();
    }

    // ── US-05: Rechazar solicitud ──────────────────────────────────────
    [Fact]
    public async Task RechazarSolicitud_ActualizaEstado()
    {
        var svc = CrearServicio("TestRechazar");
        await svc.RegistrarVisitanteAsync(
            new CrearVisitanteRequest("Tomás Vega", 1, "RECHAZA001",
                null, null, 1, 1, true, null), "127.0.0.1");

        var solicitudes = await svc.ObtenerSolicitudesPendientesAsync();
        var ok = await svc.RechazarSolicitudAsync(
            new RechazarSolicitudRequest(
                solicitudes.First().SolicitudId, 1, "Documentación incompleta"));

        ok.Should().BeTrue();

        var activosPostRechazo = await svc.ObtenerAccesosActivosAsync();
        activosPostRechazo.Should().BeEmpty();
    }
}
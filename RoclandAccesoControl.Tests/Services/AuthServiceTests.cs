using FluentAssertions;
using Microsoft.Extensions.Configuration;
using RoclandAccesoControl.Tests.Helpers;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services;

namespace RoclandAccesoControl.Tests.Services;

public class AuthServiceTests
{
    private AuthService CrearServicio()
    {
        var ctx = DbContextHelper.CrearContexto("TestAuth");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ClaveSecretaDePrueba32CaracteresMinimo!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpirationHours"] = "8"
            })
            .Build();
        return new AuthService(ctx, config);
    }

    [Fact]
    public async Task LoginGuardia_CredencialesCorrectas_RetornaToken()
    {
        var svc = CrearServicio();
        var result = await svc.LoginGuardiaAsync(
            new LoginRequest("guardia_test", "Test123!"));

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Rol.Should().Be("Guardia");
    }

    [Fact]
    public async Task LoginGuardia_PasswordIncorrecta_RetornaNull()
    {
        var svc = CrearServicio();
        var result = await svc.LoginGuardiaAsync(
            new LoginRequest("guardia_test", "WrongPassword"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginGuardia_UsuarioInexistente_RetornaNull()
    {
        var svc = CrearServicio();
        var result = await svc.LoginGuardiaAsync(
            new LoginRequest("no_existe", "cualquier"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAdmin_CredencialesCorrectas_RetornaToken()
    {
        var svc = CrearServicio();
        var result = await svc.LoginAdminAsync(
            new LoginRequest("admin_test", "Admin123!"));

        result.Should().NotBeNull();
        result!.Rol.Should().Be("Admin");
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAdmin_CredencialesIncorrectas_RetornaNull()
    {
        var svc = CrearServicio();
        var result = await svc.LoginAdminAsync(
            new LoginRequest("admin_test", "Incorrecta!"));
        result.Should().BeNull();
    }
}
using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginGuardiaAsync(LoginRequest request);
    Task<LoginResponse?> LoginAdminAsync(LoginRequest request);
}
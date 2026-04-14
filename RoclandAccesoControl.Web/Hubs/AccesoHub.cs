using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Hubs;

[Authorize(AuthenticationSchemes = "AdminCookie,Bearer", Roles = "Guardia,Admin,Supervisor")]
public class AccesoHub : Hub
{
    // El servidor llama a este método para notificar a todos los guardias conectados
    public async Task UnirseAGuardias()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Guardias");
    }

    // La app móvil llama a este método para confirmar que recibió la solicitud
    public async Task ConfirmarRecepcion(int solicitudId)
    {
        await Clients.Group("Guardias")
            .SendAsync("SolicitudConfirmada", solicitudId);
    }

    public async Task NotificarSalida(int registroId)
    {
        // Enviamos el ID a todos los miembros del grupo "Guardias"
        // El cliente móvil debe estar escuchando el evento "SalidaRegistrada"
        await Clients.Group("Guardias").SendAsync("SalidaRegistrada", registroId);
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Guardias");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Guardias");
        await base.OnDisconnectedAsync(exception);
    }


}
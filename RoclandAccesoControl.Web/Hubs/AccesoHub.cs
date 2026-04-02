using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Hubs;

[Authorize(Roles = "Guardia")]
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
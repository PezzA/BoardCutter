using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoardCutter.ApiService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", Context.User?.Identity?.Name ?? "unknown", message);
    }
}
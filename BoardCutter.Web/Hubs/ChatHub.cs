using BoardCutter.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoardCutter.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        if (Context.User?.Claims != null)
        {
            await Clients.All.SendAsync("ReceiveMessage", UserExtensions.GetUserId(Context.User.Claims), message);
        }
    }
}
using Microsoft.AspNetCore.SignalR;
using Umbraco.Cms.Core.Security;

public class TestHub : Hub<ITestHubEvents>
{
    private readonly IMemberManager _memberManager;

    public TestHub(IMemberManager memberManager)
    {
        _memberManager = memberManager;
    }
    // when a client sends us a ping
    public async Task Ping()
    {

        // we trigger the pong event on all clients
        await Clients.Caller.Pong(message: Context?.User?.Identity?.Name ?? "na");
    }
}

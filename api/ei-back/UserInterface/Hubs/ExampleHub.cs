using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ei_back.UserInterface.Hubs
{
    public class ExampleHub : Hub
    {
        [Authorize(Roles = "Admin")]
        public async Task TestHub(string parameterTest)
        {
            await Clients.All.SendAsync($"ReceiveMessage - {parameterTest}");
        }

        [Authorize(Roles = "Admin")]
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync($"ReceiveMessage - {Context.ConnectionId} has joined");
        }
    }
}

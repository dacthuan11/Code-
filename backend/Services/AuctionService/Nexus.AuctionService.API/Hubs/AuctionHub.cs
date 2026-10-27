using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexus.AuctionService.API.Hubs;

[Authorize]
public class AuctionHub : Hub
{
    public async Task JoinAuction(string auctionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");

    public async Task LeaveAuction(string auctionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
}

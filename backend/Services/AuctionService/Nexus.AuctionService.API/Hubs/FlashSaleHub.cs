using Microsoft.AspNetCore.SignalR;

namespace Nexus.AuctionService.API.Hubs;

public class FlashSaleHub : Hub
{
    public async Task JoinFlashSale(string flashSaleId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"flashsale-{flashSaleId}");

    public async Task LeaveFlashSale(string flashSaleId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"flashsale-{flashSaleId}");
}

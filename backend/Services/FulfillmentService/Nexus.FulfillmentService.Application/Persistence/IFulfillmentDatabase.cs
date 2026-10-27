using Microsoft.EntityFrameworkCore;
using Nexus.FulfillmentService.Domain.Entities;

namespace Nexus.FulfillmentService.Application.Persistence;

public interface IFulfillmentDatabase
{
    DbSet<Shipment>              Shipments              { get; }
    DbSet<ReturnRequest>         ReturnRequests         { get; }
    DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Nexus.Shared.Domain;

namespace Nexus.FulfillmentService.Domain.Entities;

public class ShipmentTrackingEvent : BaseEntity
{
    public Guid   ShipmentId  { get; private set; }
    public string Status      { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? Location   { get; private set; }

    private ShipmentTrackingEvent() { }

    public static ShipmentTrackingEvent Create(Guid shipmentId, string status, string description, string? location = null)
        => new() { ShipmentId = shipmentId, Status = status, Description = description, Location = location };
}

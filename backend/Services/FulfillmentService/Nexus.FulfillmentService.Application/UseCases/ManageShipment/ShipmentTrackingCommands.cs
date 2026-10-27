using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.FulfillmentService.Domain.Entities;
using Nexus.FulfillmentService.Application.Persistence;

namespace Nexus.FulfillmentService.Application.UseCases.ManageShipment;

public record TrackingEventDto(Guid Id, string Status, string Description, string? Location, DateTime CreatedAt);

public record AddTrackingEventCommand(Guid ShipmentId, string Status, string Description, string? Location = null) : IRequest;

public class AddTrackingEventHandler : IRequestHandler<AddTrackingEventCommand>
{
    private readonly IFulfillmentDatabase _db;
    public AddTrackingEventHandler(IFulfillmentDatabase db) => _db = db;

    public async Task Handle(AddTrackingEventCommand cmd, CancellationToken ct)
    {
        var exists = await _db.Shipments.AnyAsync(s => s.Id == cmd.ShipmentId, ct);
        if (!exists) throw new KeyNotFoundException("Không tìm thấy vận đơn.");
        var evt = ShipmentTrackingEvent.Create(cmd.ShipmentId, cmd.Status, cmd.Description, cmd.Location);
        _db.ShipmentTrackingEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
    }
}

public record GetShipmentTrackingQuery(Guid ShipmentId) : IRequest<List<TrackingEventDto>>;

public class GetShipmentTrackingHandler : IRequestHandler<GetShipmentTrackingQuery, List<TrackingEventDto>>
{
    private readonly IFulfillmentDatabase _db;
    public GetShipmentTrackingHandler(IFulfillmentDatabase db) => _db = db;

    public async Task<List<TrackingEventDto>> Handle(GetShipmentTrackingQuery q, CancellationToken ct)
        => await _db.ShipmentTrackingEvents
            .Where(t => t.ShipmentId == q.ShipmentId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TrackingEventDto(t.Id, t.Status, t.Description, t.Location, t.CreatedAt))
            .ToListAsync(ct);
}

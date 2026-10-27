using MediatR;
using Nexus.FulfillmentService.Domain.Repositories;

namespace Nexus.FulfillmentService.Application.UseCases.GetShipment;

public class GetShipmentByIdHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto?>
{
    private readonly IShipmentRepository _shipments;
    public GetShipmentByIdHandler(IShipmentRepository shipments) => _shipments = shipments;

    public async Task<ShipmentDto?> Handle(GetShipmentByIdQuery q, CancellationToken ct) =>
        ShipmentDtoMapper.ToDto(await _shipments.GetByIdAsync(q.ShipmentId, ct));
}

public class GetShipmentByOrderIdHandler : IRequestHandler<GetShipmentByOrderIdQuery, ShipmentDto?>
{
    private readonly IShipmentRepository _shipments;
    public GetShipmentByOrderIdHandler(IShipmentRepository shipments) => _shipments = shipments;

    public async Task<ShipmentDto?> Handle(GetShipmentByOrderIdQuery q, CancellationToken ct) =>
        ShipmentDtoMapper.ToDto(await _shipments.GetByOrderIdAsync(q.OrderId, ct));
}

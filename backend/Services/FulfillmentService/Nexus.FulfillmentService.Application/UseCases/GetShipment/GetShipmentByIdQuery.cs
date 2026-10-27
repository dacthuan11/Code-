using MediatR;
using Nexus.FulfillmentService.Domain.Entities;

namespace Nexus.FulfillmentService.Application.UseCases.GetShipment;

public record GetShipmentByIdQuery(Guid ShipmentId) : IRequest<ShipmentDto?>;
public record GetShipmentByOrderIdQuery(Guid OrderId) : IRequest<ShipmentDto?>;

public record ShipmentDto(
    Guid Id,
    Guid OrderId,
    string Carrier,
    string? TrackingNumber,
    decimal ShippingFee,
    string Status,
    string ShippingAddress,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class ShipmentDtoMapper
{
    public static ShipmentDto? ToDto(Shipment? s) => s is null ? null : new ShipmentDto(
        s.Id, s.OrderId, s.Carrier, s.TrackingNumber,
        s.ShippingFee, s.Status.ToString(), s.ShippingAddress,
        s.CreatedAt, s.UpdatedAt);
}

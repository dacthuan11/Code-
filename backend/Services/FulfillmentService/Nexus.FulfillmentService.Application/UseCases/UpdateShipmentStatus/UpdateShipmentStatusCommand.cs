using MediatR;
using Nexus.FulfillmentService.Domain.Entities;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.UseCases.UpdateShipmentStatus;

public record UpdateShipmentStatusCommand(
    Guid ShipmentId,
    string Status,
    string? DeliveryNote = null,
    string BuyerEmail = "",
    string RecipientName = ""
) : IRequest<bool>;

public class UpdateShipmentStatusHandler : IRequestHandler<UpdateShipmentStatusCommand, bool>
{
    private readonly IShipmentRepository _shipments;
    private readonly IEventBus _eventBus;

    public UpdateShipmentStatusHandler(IShipmentRepository shipments, IEventBus eventBus)
    {
        _shipments = shipments;
        _eventBus = eventBus;
    }

    public async Task<bool> Handle(UpdateShipmentStatusCommand cmd, CancellationToken ct)
    {
        var shipment = await _shipments.GetByIdAsync(cmd.ShipmentId, ct);
        if (shipment is null) return false;

        if (!Enum.TryParse<ShipmentStatus>(cmd.Status, true, out var status))
            return false;

        switch (status)
        {
            case ShipmentStatus.PickedUp:
                shipment.PickUp();
                break;

            case ShipmentStatus.InTransit:
                shipment.Transit();
                break;

            case ShipmentStatus.OutForDelivery:
                shipment.StartDelivery();
                break;

            case ShipmentStatus.Delivered:
                shipment.Deliver();
                await _eventBus.PublishAsync(new ShipmentDeliveredEvent(
                    shipment.Id, shipment.OrderId, shipment.BuyerId,
                    shipment.TrackingNumber ?? "", cmd.BuyerEmail, cmd.RecipientName), ct);
                // Notify CommerceService to mark order Completed
                try
                {
                    using var http = new System.Net.Http.HttpClient();
                    await http.PostAsync(
                        $"http://localhost:5003/api/v1/orders/{shipment.OrderId}/complete",
                        new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json"), ct);
                }
                catch { /* non-critical */ }
                break;

            case ShipmentStatus.Failed:
                shipment.Fail(cmd.DeliveryNote);
                break;

            case ShipmentStatus.Returned:
                shipment.Return();
                break;

            case ShipmentStatus.ReturnedToSender:
                shipment.ReturnComplete();
                break;

            default:
                return false;
        }

        await _shipments.UpdateAsync(shipment, ct);
        await _shipments.SaveChangesAsync(ct);
        return true;
    }
}

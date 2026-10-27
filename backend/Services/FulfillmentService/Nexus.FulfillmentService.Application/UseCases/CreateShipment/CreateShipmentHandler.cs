using MediatR;
using Nexus.FulfillmentService.Domain.Entities;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.UseCases.CreateShipment;

/// <summary>
/// Xử lý tạo đơn giao hàng - tương tác với carrier API để lấy tracking number.
/// Flow: gọi carrier API → tạo Shipment entity → lưu DB → publish event.
/// Nếu carrier API fail thì throw exception và không lưu DB (fail fast).
/// </summary>
public class CreateShipmentHandler : IRequestHandler<CreateShipmentCommand, CreateShipmentResult>
{
    private readonly IShipmentRepository _shipments;
    private readonly IShippingCarrier _carrier;  // Abstraction - dễ swap GHN sang GHTK sau này
    private readonly IEventBus _eventBus;

    public CreateShipmentHandler(IShipmentRepository shipments, IShippingCarrier carrier, IEventBus eventBus)
    {
        _shipments = shipments;
        _carrier = carrier;
        _eventBus = eventBus;
    }

    public async Task<CreateShipmentResult> Handle(CreateShipmentCommand cmd, CancellationToken ct)
    {
        // Gọi carrier API trước - nếu lỗi thì không cần làm gì thêm
        var (trackingNumber, fee) = await _carrier.CreateShipmentAsync(cmd, ct);

        // Ghép địa chỉ đầy đủ theo format: Tên, SĐT, Địa chỉ, Phường, Quận, Tỉnh
        // Dùng Guid.Empty cho warehouse vì dev chưa setup multi-warehouse
        // TODO: Sau này lấy warehouseId từ inventory assignment thay vì hardcode Empty
        var fullAddress = $"{cmd.RecipientName}, {cmd.Phone}, {cmd.Address}, {cmd.Ward}, {cmd.District}, {cmd.Province}";
        var shipment = Shipment.Create(cmd.OrderId, cmd.BuyerId, Guid.Empty, "GHN", fee, fullAddress);

        // Gán tracking ngay sau khi tạo entity - không để null vì carrier đã trả về rồi
        shipment.AssignTracking(trackingNumber);

        await _shipments.AddAsync(shipment, ct);
        await _shipments.SaveChangesAsync(ct);

        // Publish event để các service khác (NotificationService, OrderService) biết
        await _eventBus.PublishAsync(new ShipmentCreatedEvent(
            shipment.Id, cmd.OrderId, trackingNumber, "GHN",
            cmd.BuyerId, cmd.BuyerEmail, cmd.RecipientName, cmd.ExpectedDelivery), ct);

        return new CreateShipmentResult(shipment.Id, trackingNumber, fee);
    }
}

/// <summary>
/// Port interface cho shipping carrier - theo Hexagonal Architecture.
/// Giúp tách business logic khỏi implementation cụ thể của từng carrier.
/// </summary>
public interface IShippingCarrier
{
    Task<(string TrackingNumber, decimal Fee)> CreateShipmentAsync(CreateShipmentCommand cmd, CancellationToken ct);
    Task<string> GetTrackingStatusAsync(string trackingNumber, CancellationToken ct);
    Task<ShippingFeeEstimate> CalculateFeeAsync(ShippingFeeRequest req, CancellationToken ct);
}

public record ShippingFeeRequest(
    int ToDistrictId, string ToWardCode, long InsuranceValue,
    int Weight = 500, int Length = 20, int Width = 15, int Height = 10);

public record ShippingFeeEstimate(long TotalFee, long ServiceFee, long InsuranceFee, string ServiceType);

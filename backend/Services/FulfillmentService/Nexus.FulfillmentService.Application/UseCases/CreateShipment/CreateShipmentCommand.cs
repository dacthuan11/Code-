using MediatR;

namespace Nexus.FulfillmentService.Application.UseCases.CreateShipment;

/// <summary>
/// Command tạo đơn giao hàng qua carrier (hiện tại GHN).
/// Nhận đủ thông tin địa chỉ để ghép thành địa chỉ giao hàng đầy đủ.
/// DeclaredValue dùng để tính phí vận chuyển và giá trị bảo hiểm.
/// </summary>
public record CreateShipmentCommand(
    Guid OrderId,
    Guid BuyerId,
    string RecipientName,
    string Phone,
    string Address,
    string Province,
    string District,
    string Ward,
    int ToDistrictId,
    string ToWardCode,
    decimal DeclaredValue,
    string BuyerEmail = "",
    DateTime? ExpectedDelivery = null
) : IRequest<CreateShipmentResult>;

/// <summary>
/// Kết quả trả về sau khi tạo shipment thành công.
/// TrackingNumber dùng để theo dõi đơn hàng trên trang của carrier.
/// ShippingFee lấy trực tiếp từ carrier API thay vì tính tay để đảm bảo chính xác.
/// </summary>
public record CreateShipmentResult(Guid ShipmentId, string TrackingNumber, decimal ShippingFee);

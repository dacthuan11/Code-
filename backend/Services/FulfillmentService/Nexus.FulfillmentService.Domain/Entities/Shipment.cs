using Nexus.Shared.Domain;

namespace Nexus.FulfillmentService.Domain.Entities;

// Vòng đời shipment - khớp 1-1 với GHN status flow
public enum ShipmentStatus
{
    Created        = 0,  // Đơn tạo, chờ GHN lấy
    PickedUp       = 1,  // GHN đã lấy hàng tại kho
    InTransit      = 2,  // Đang trên đường vận chuyển
    OutForDelivery = 3,  // Đang giao đến người nhận (shipper tại địa chỉ)
    Delivered      = 4,  // Giao thành công
    Failed         = 5,  // Giao thất bại (không liên lạc được / từ chối)
    Returned       = 6,  // Đang hoàn hàng về kho
    ReturnedToSender = 7 // Đã hoàn về kho người gửi
}

public class Shipment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid WarehouseId { get; private set; }

    public string Carrier { get; private set; } = default!;
    public string? TrackingNumber { get; private set; }
    public decimal ShippingFee { get; private set; }
    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Created;
    public string ShippingAddress { get; private set; } = default!;

    // Thời gian giao dự kiến - lấy từ GHN expected_delivery_time
    public DateTime? ExpectedDeliveryTime { get; private set; }

    // Ghi chú giao hàng (lý do thất bại, v.v.)
    public string? DeliveryNote { get; private set; }

    private Shipment() { }

    public static Shipment Create(Guid orderId, Guid buyerId, Guid warehouseId, string carrier,
        decimal fee, string address, DateTime? expectedDelivery = null) => new()
    {
        OrderId = orderId, BuyerId = buyerId, WarehouseId = warehouseId,
        Carrier = carrier, ShippingFee = fee, ShippingAddress = address,
        ExpectedDeliveryTime = expectedDelivery,
    };

    public void AssignTracking(string trackingNumber, DateTime? expectedDelivery = null)
    {
        TrackingNumber = trackingNumber;
        if (expectedDelivery.HasValue) ExpectedDeliveryTime = expectedDelivery;
        Touch();
    }

    public void PickUp()          { Status = ShipmentStatus.PickedUp; Touch(); }
    public void Transit()         { Status = ShipmentStatus.InTransit; Touch(); }
    public void StartDelivery()   { Status = ShipmentStatus.OutForDelivery; Touch(); }
    public void Deliver()         { Status = ShipmentStatus.Delivered; Touch(); }
    public void Fail(string? note = null) { Status = ShipmentStatus.Failed; DeliveryNote = note; Touch(); }
    public void Return()          { Status = ShipmentStatus.Returned; Touch(); }
    public void ReturnComplete()  { Status = ShipmentStatus.ReturnedToSender; Touch(); }
}

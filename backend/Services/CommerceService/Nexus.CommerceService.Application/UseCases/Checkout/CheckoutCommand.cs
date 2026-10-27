using MediatR;
using Nexus.CommerceService.Domain.Entities;

namespace Nexus.CommerceService.Application.UseCases.Checkout;

public record CheckoutCommand(
    Guid UserId,
    string UserEmail,
    string RecipientName,
    string Phone,
    string Address,
    string Province,
    string District,
    string Ward,
    int ToDistrictId,
    string ToWardCode,
    decimal ShippingFee,
    string PaymentMethod,         // "COD" | "VNPAY" | "MOMO"
    DeliveryType DeliveryType = DeliveryType.Standard,
    int ServiceTypeId = 2,        // GHN: 1=express, 2=standard, 3=economy
    bool IsInternational = false  // hàng quốc tế → PreOrder flow
) : IRequest<CheckoutResult>;

public record CheckoutResult(
    Guid OrderId,
    string? PaymentUrl,
    decimal TotalAmount,
    DateTime? EstimatedDelivery,
    string DeliveryTypeLabel
);

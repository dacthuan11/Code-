using MediatR;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CommerceService.Application.UseCases.Checkout;

public class CheckoutHandler : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private readonly ICartRepository _carts;
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _payment;
    private readonly IEventBus _eventBus;

    public CheckoutHandler(ICartRepository carts, IOrderRepository orders,
        IPaymentGateway payment, IEventBus eventBus)
    {
        _carts = carts; _orders = orders; _payment = payment; _eventBus = eventBus;
    }

    public async Task<CheckoutResult> Handle(CheckoutCommand cmd, CancellationToken ct)
    {
        var cart = await _carts.GetActiveByUserIdAsync(cmd.UserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy giỏ hàng.");

        if (!cart.Items.Any()) throw new InvalidOperationException("Giỏ hàng trống.");

        var fullAddress = $"{cmd.RecipientName}, {cmd.Phone}, {cmd.Address}, {cmd.Ward}, {cmd.District}, {cmd.Province}";

        var orderItems = cart.Items
            .Select(i => OrderItem.Create(Guid.Empty, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity))
            .ToList();

        // Xác định loại giao hàng: nếu hàng quốc tế → PreOrder
        var deliveryType = cmd.IsInternational ? DeliveryType.PreOrder : cmd.DeliveryType;

        var order = Order.Create(
            cmd.UserId, Guid.Empty, fullAddress, cmd.ShippingFee,
            orderItems, auctionId: null,
            deliveryType: deliveryType);

        await _orders.AddAsync(order, ct);

        cart.MarkDeleted();

        string? paymentUrl = null;

        if (cmd.PaymentMethod == "COD")
        {
            // COD và hàng PreOrder: tự động Confirm sau khi seller xem
            if (deliveryType == DeliveryType.PreOrder)
                order.Confirm(); // trạng thái PreOrder → seller confirm sau khi hàng về kho
            // Còn lại (Standard/Express COD): giữ PendingPayment cho đến khi seller confirm
        }
        else
        {
            paymentUrl = await _payment.CreatePaymentSessionAsync(order, ct);
        }

        await _orders.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new OrderCreatedEvent(order.Id, order.UserId, Guid.Empty, order.TotalAmount, cmd.UserEmail), ct);

        var label = deliveryType switch
        {
            DeliveryType.Express  => "Hỏa tốc (trong ngày)",
            DeliveryType.PreOrder => "Hàng nhập khẩu / đặt trước",
            _                     => "Giao hàng tiêu chuẩn",
        };

        return new CheckoutResult(order.Id, paymentUrl, order.TotalAmount, order.EstimatedDelivery, label);
    }
}

public interface IPaymentGateway
{
    Task<string> CreatePaymentSessionAsync(Order order, CancellationToken ct);
    Task<bool> VerifyPaymentAsync(string paymentIntentId, CancellationToken ct);
    Task<bool> RefundAsync(string paymentIntentId, decimal amount, CancellationToken ct);
}

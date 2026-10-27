using MediatR;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Domain.Repositories;

namespace Nexus.CommerceService.Application.UseCases.GetOrders;

public record GetOrdersQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<GetOrdersResult>;

public record OrderSummaryDto(
    Guid OrderId,
    string Status,
    string DeliveryType,
    decimal TotalAmount,
    decimal ShippingFee,
    string ShippingAddress,
    string? PaymentRef,
    string? CancellationReason,
    DateTime? EstimatedDelivery,
    Guid? AuctionId,
    DateTime CreatedAt,
    List<OrderItemDto> Items);

public record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public record GetOrdersResult(List<OrderSummaryDto> Items, int TotalCount, int Page, int PageSize);

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, GetOrdersResult>
{
    private readonly IOrderRepository _orders;
    public GetOrdersHandler(IOrderRepository orders) => _orders = orders;

    public async Task<GetOrdersResult> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var (items, total) = await _orders.GetByUserIdAsync(q.UserId, q.Page, q.PageSize, ct);

        var dtos = items.Select(o => new OrderSummaryDto(
            o.Id,
            o.Status.ToString(),
            o.DeliveryType.ToString(),
            o.TotalAmount,
            o.ShippingFee,
            o.ShippingAddress,
            o.PaymentRef,
            o.CancellationReason,
            o.EstimatedDelivery,
            o.AuctionId,
            o.CreatedAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList()
        )).ToList();

        return new GetOrdersResult(dtos, total, q.Page, q.PageSize);
    }
}

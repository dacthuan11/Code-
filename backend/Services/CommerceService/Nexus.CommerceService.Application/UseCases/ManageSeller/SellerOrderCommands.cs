using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Application.Persistence;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CommerceService.Application.UseCases.ManageSeller;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record SellerOrderDto(
    Guid Id, string Status, decimal TotalAmount, DateTime CreatedAt,
    List<SellerOrderItemDto> Items);

public record SellerOrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

// ── Confirm Order ─────────────────────────────────────────────────────────────
public record ConfirmOrderCommand(Guid OrderId, Guid SellerId) : IRequest;

public class ConfirmOrderHandler : IRequestHandler<ConfirmOrderCommand>
{
    private readonly ICommerceDatabase _db;
    private readonly IEventBus _eventBus;
    public ConfirmOrderHandler(ICommerceDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(ConfirmOrderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId && o.SellerId == cmd.SellerId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        order.Confirm();
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new OrderConfirmedEvent(cmd.OrderId, cmd.SellerId), ct);
    }
}

// ── Mark Ready to Ship ────────────────────────────────────────────────────────
public record MarkReadyToShipCommand(Guid OrderId, Guid SellerId) : IRequest;

public class MarkReadyToShipHandler : IRequestHandler<MarkReadyToShipCommand>
{
    private readonly ICommerceDatabase _db;
    private readonly IEventBus _eventBus;
    public MarkReadyToShipHandler(ICommerceDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(MarkReadyToShipCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId && o.SellerId == cmd.SellerId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        order.StartProcessing();
        order.MarkReadyToShip();
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new OrderReadyToShipEvent(
            cmd.OrderId, cmd.SellerId, "", "", "", "", "", "", 0, ""), ct);
    }
}

// ── Mark Delivered ────────────────────────────────────────────────────────────
public record MarkDeliveredCommand(Guid OrderId, Guid SellerId) : IRequest;

public class MarkDeliveredHandler : IRequestHandler<MarkDeliveredCommand>
{
    private readonly ICommerceDatabase _db;
    private readonly IEventBus _eventBus;
    public MarkDeliveredHandler(ICommerceDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(MarkDeliveredCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId && o.SellerId == cmd.SellerId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        order.MarkDelivered();
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new OrderDeliveredEvent(cmd.OrderId, order.UserId), ct);
    }
}

// ── Get Seller Orders ─────────────────────────────────────────────────────────
public record GetSellerOrdersQuery(Guid SellerId, string? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<(List<SellerOrderDto> Items, int Total)>;

public class GetSellerOrdersHandler : IRequestHandler<GetSellerOrdersQuery, (List<SellerOrderDto>, int)>
{
    private readonly ICommerceDatabase _db;
    public GetSellerOrdersHandler(ICommerceDatabase db) => _db = db;

    public async Task<(List<SellerOrderDto>, int)> Handle(GetSellerOrdersQuery q, CancellationToken ct)
    {
        var query = _db.Orders.Include(o => o.Items)
            .Where(o => o.SellerId == q.SellerId);

        if (!string.IsNullOrEmpty(q.Status) && Enum.TryParse<OrderStatus>(q.Status, true, out var statusEnum))
            query = query.Where(o => o.Status == statusEnum);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(o => new SellerOrderDto(
                o.Id, o.Status.ToString(), o.TotalAmount, o.CreatedAt,
                o.Items.Select(i => new SellerOrderItemDto(
                    i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList()))
            .ToListAsync(ct);
        return (items, total);
    }
}

// ── Get Seller Analytics ──────────────────────────────────────────────────────
public record GetSellerAnalyticsQuery(Guid SellerId, DateTime? From = null, DateTime? To = null)
    : IRequest<SellerAnalyticsDto>;

public record SellerAnalyticsDto(
    int TotalOrders, decimal TotalRevenue, decimal AverageOrderValue,
    int PendingOrders, int CompletedOrders, int CancelledOrders);

public class GetSellerAnalyticsHandler : IRequestHandler<GetSellerAnalyticsQuery, SellerAnalyticsDto>
{
    private readonly ICommerceDatabase _db;
    public GetSellerAnalyticsHandler(ICommerceDatabase db) => _db = db;

    public async Task<SellerAnalyticsDto> Handle(GetSellerAnalyticsQuery q, CancellationToken ct)
    {
        var from = q.From ?? DateTime.UtcNow.AddMonths(-1);
        var to = q.To ?? DateTime.UtcNow;
        var orders = await _db.Orders
            .Where(o => o.SellerId == q.SellerId && o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync(ct);

        return new SellerAnalyticsDto(
            orders.Count,
            orders.Sum(o => o.TotalAmount),
            orders.Count > 0 ? orders.Average(o => o.TotalAmount) : 0,
            orders.Count(o => o.Status == OrderStatus.PendingPayment),
            orders.Count(o => o.Status == OrderStatus.Completed),
            orders.Count(o => o.Status == OrderStatus.Cancelled));
    }
}

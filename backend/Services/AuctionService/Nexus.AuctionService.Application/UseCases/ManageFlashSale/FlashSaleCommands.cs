using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.AuctionService.Domain.Entities;
using Nexus.AuctionService.Application.Persistence;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.AuctionService.Application.UseCases.ManageFlashSale;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record FlashSaleItemDto(Guid Id, Guid ProductId, Guid SkuId,
    decimal OriginalPrice, decimal FlashSalePrice, int TotalSlots, int SoldSlots, int RemainingSlots);

public record FlashSaleDto(Guid Id, string Name, DateTime StartTime, DateTime EndTime,
    string Status, Guid SellerId, List<FlashSaleItemDto> Items);

// ── Create Flash Sale ─────────────────────────────────────────────────────────
public record FlashSaleItemRequest(Guid ProductId, Guid SkuId,
    decimal OriginalPrice, decimal FlashSalePrice, int TotalSlots);

public record CreateFlashSaleCommand(
    string Name, DateTime StartTime, DateTime EndTime, Guid SellerId,
    List<FlashSaleItemRequest> Items) : IRequest<Guid>;

public class CreateFlashSaleHandler : IRequestHandler<CreateFlashSaleCommand, Guid>
{
    private readonly IAuctionDatabase _db;
    public CreateFlashSaleHandler(IAuctionDatabase db) => _db = db;

    public async Task<Guid> Handle(CreateFlashSaleCommand cmd, CancellationToken ct)
    {
        var fs = FlashSale.Create(cmd.Name, cmd.StartTime, cmd.EndTime, cmd.SellerId);
        foreach (var item in cmd.Items)
        {
            var fsItem = FlashSaleItem.Create(fs.Id, item.ProductId, item.SkuId,
                item.OriginalPrice, item.FlashSalePrice, item.TotalSlots);
            fs.AddItem(fsItem);
        }
        _db.FlashSales.Add(fs);
        await _db.SaveChangesAsync(ct);
        return fs.Id;
    }
}

// ── Activate Flash Sale ───────────────────────────────────────────────────────
public record ActivateFlashSaleCommand(Guid FlashSaleId, Guid SellerId) : IRequest;

public class ActivateFlashSaleHandler : IRequestHandler<ActivateFlashSaleCommand>
{
    private readonly IAuctionDatabase _db;
    private readonly IEventBus _eventBus;
    public ActivateFlashSaleHandler(IAuctionDatabase db, IEventBus eventBus)
    { _db = db; _eventBus = eventBus; }

    public async Task Handle(ActivateFlashSaleCommand cmd, CancellationToken ct)
    {
        var fs = await _db.FlashSales.Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.Id == cmd.FlashSaleId && f.SellerId == cmd.SellerId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy Flash Sale.");
        fs.Activate();
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new FlashSaleStartedEvent(
            fs.Id, fs.Name, fs.EndTime, fs.Items.Select(i => i.SkuId).ToList()), ct);
    }
}

// ── Get Active Flash Sales ────────────────────────────────────────────────────
public record GetActiveFlashSalesQuery(int Page = 1, int PageSize = 10)
    : IRequest<List<FlashSaleDto>>;

public class GetActiveFlashSalesHandler : IRequestHandler<GetActiveFlashSalesQuery, List<FlashSaleDto>>
{
    private readonly IAuctionDatabase _db;
    public GetActiveFlashSalesHandler(IAuctionDatabase db) => _db = db;

    public async Task<List<FlashSaleDto>> Handle(GetActiveFlashSalesQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.FlashSales.Include(f => f.Items)
            .Where(f => f.Status == FlashSaleStatus.Active && f.StartTime <= now && f.EndTime >= now)
            .OrderBy(f => f.EndTime)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(f => new FlashSaleDto(f.Id, f.Name, f.StartTime, f.EndTime,
                f.Status.ToString(), f.SellerId,
                f.Items.Select(i => new FlashSaleItemDto(i.Id, i.ProductId, i.SkuId,
                    i.OriginalPrice, i.FlashSalePrice, i.TotalSlots, i.SoldSlots,
                    i.RemainingSlots)).ToList()))
            .ToListAsync(ct);
    }
}

// ── Reserve Flash Sale Slot ───────────────────────────────────────────────────
public record ReserveFlashSaleSlotCommand(Guid FlashSaleItemId) : IRequest<bool>;

public class ReserveFlashSaleSlotHandler : IRequestHandler<ReserveFlashSaleSlotCommand, bool>
{
    private readonly IAuctionDatabase _db;
    private readonly IEventBus _eventBus;
    public ReserveFlashSaleSlotHandler(IAuctionDatabase db, IEventBus eventBus)
    { _db = db; _eventBus = eventBus; }

    public async Task<bool> Handle(ReserveFlashSaleSlotCommand cmd, CancellationToken ct)
    {
        var item = await _db.FlashSaleItems
            .FirstOrDefaultAsync(i => i.Id == cmd.FlashSaleItemId, ct)
            ?? throw new KeyNotFoundException("Flash Sale item không tồn tại.");

        if (!item.TryReserve()) return false;
        await _db.SaveChangesAsync(ct);

        if (item.RemainingSlots <= 5)
            await _eventBus.PublishAsync(
                new FlashSaleSlotRunningLowEvent(item.FlashSaleId, item.SkuId, item.RemainingSlots), ct);

        return true;
    }
}

// ── Get Flash Sale by ID ──────────────────────────────────────────────────────
public record GetFlashSaleQuery(Guid FlashSaleId) : IRequest<FlashSaleDto?>;

public class GetFlashSaleHandler : IRequestHandler<GetFlashSaleQuery, FlashSaleDto?>
{
    private readonly IAuctionDatabase _db;
    public GetFlashSaleHandler(IAuctionDatabase db) => _db = db;

    public async Task<FlashSaleDto?> Handle(GetFlashSaleQuery q, CancellationToken ct)
    {
        var f = await _db.FlashSales.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == q.FlashSaleId, ct);
        if (f is null) return null;
        return new FlashSaleDto(f.Id, f.Name, f.StartTime, f.EndTime,
            f.Status.ToString(), f.SellerId,
            f.Items.Select(i => new FlashSaleItemDto(i.Id, i.ProductId, i.SkuId,
                i.OriginalPrice, i.FlashSalePrice, i.TotalSlots, i.SoldSlots,
                i.RemainingSlots)).ToList());
    }
}

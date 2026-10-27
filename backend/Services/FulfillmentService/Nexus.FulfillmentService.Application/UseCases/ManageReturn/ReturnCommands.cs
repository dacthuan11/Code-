using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.FulfillmentService.Domain.Entities;
using Nexus.FulfillmentService.Application.Persistence;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.FulfillmentService.Application.UseCases.ManageReturn;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record ReturnRequestDto(
    Guid Id, Guid OrderId, Guid SellerId, Guid BuyerId,
    string Reason, string Description, string Status,
    decimal RefundAmount, string? SellerNote, DateTime? ResolvedAt,
    List<string> EvidenceImageUrls, DateTime CreatedAt);

// ── Create Return Request ─────────────────────────────────────────────────────
public record CreateReturnRequestCommand(
    Guid OrderId, Guid SellerId, Guid BuyerId,
    string Reason, string Description, decimal RefundAmount,
    List<string>? EvidenceImageUrls = null) : IRequest<Guid>;

public class CreateReturnRequestHandler : IRequestHandler<CreateReturnRequestCommand, Guid>
{
    private readonly IFulfillmentDatabase _db;
    private readonly IEventBus _eventBus;
    public CreateReturnRequestHandler(IFulfillmentDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task<Guid> Handle(CreateReturnRequestCommand cmd, CancellationToken ct)
    {
        var alreadyExists = await _db.ReturnRequests
            .AnyAsync(r => r.OrderId == cmd.OrderId && r.BuyerId == cmd.BuyerId
                && r.Status == ReturnStatus.Pending, ct);
        if (alreadyExists) throw new InvalidOperationException("Đã có yêu cầu hoàn trả đang chờ xử lý.");

        if (!Enum.TryParse<ReturnReason>(cmd.Reason, true, out var reason))
            reason = ReturnReason.Other;

        var ret = ReturnRequest.Create(cmd.OrderId, cmd.SellerId, cmd.BuyerId,
            reason, cmd.Description, cmd.RefundAmount, cmd.EvidenceImageUrls);
        _db.ReturnRequests.Add(ret);
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new ReturnRequestCreatedEvent(ret.Id, cmd.OrderId, cmd.SellerId), ct);
        return ret.Id;
    }
}

// ── Approve Return ────────────────────────────────────────────────────────────
public record ApproveReturnCommand(Guid ReturnId, Guid SellerId, string? Note = null) : IRequest;

public class ApproveReturnHandler : IRequestHandler<ApproveReturnCommand>
{
    private readonly IFulfillmentDatabase _db;
    private readonly IEventBus _eventBus;
    public ApproveReturnHandler(IFulfillmentDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(ApproveReturnCommand cmd, CancellationToken ct)
    {
        var ret = await _db.ReturnRequests.FirstOrDefaultAsync(r => r.Id == cmd.ReturnId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu hoàn trả.");
        if (ret.SellerId != cmd.SellerId) throw new UnauthorizedAccessException("Không có quyền xử lý yêu cầu này.");

        ret.Approve(cmd.Note);
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(
            new ReturnApprovedEvent(ret.Id, ret.OrderId, ret.BuyerId, ret.RefundAmount), ct);
    }
}

// ── Reject Return ─────────────────────────────────────────────────────────────
public record RejectReturnCommand(Guid ReturnId, Guid SellerId, string Reason) : IRequest;

public class RejectReturnHandler : IRequestHandler<RejectReturnCommand>
{
    private readonly IFulfillmentDatabase _db;
    private readonly IEventBus _eventBus;
    public RejectReturnHandler(IFulfillmentDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(RejectReturnCommand cmd, CancellationToken ct)
    {
        var ret = await _db.ReturnRequests.FirstOrDefaultAsync(r => r.Id == cmd.ReturnId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu hoàn trả.");
        if (ret.SellerId != cmd.SellerId) throw new UnauthorizedAccessException("Không có quyền xử lý yêu cầu này.");

        ret.Reject(cmd.Reason);
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(
            new ReturnRejectedEvent(ret.Id, ret.OrderId, ret.BuyerId, cmd.Reason), ct);
    }
}

// ── Get Returns by Order ──────────────────────────────────────────────────────
public record GetReturnsByOrderQuery(Guid OrderId) : IRequest<List<ReturnRequestDto>>;

public class GetReturnsByOrderHandler : IRequestHandler<GetReturnsByOrderQuery, List<ReturnRequestDto>>
{
    private readonly IFulfillmentDatabase _db;
    public GetReturnsByOrderHandler(IFulfillmentDatabase db) => _db = db;

    public async Task<List<ReturnRequestDto>> Handle(GetReturnsByOrderQuery q, CancellationToken ct)
        => await _db.ReturnRequests.Where(r => r.OrderId == q.OrderId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReturnRequestDto(r.Id, r.OrderId, r.SellerId, r.BuyerId,
                r.Reason.ToString(), r.Description, r.Status.ToString(),
                r.RefundAmount, r.SellerNote, r.ResolvedAt, r.EvidenceImageUrls, r.CreatedAt))
            .ToListAsync(ct);
}

// ── Get Seller Returns ────────────────────────────────────────────────────────
public record GetSellerReturnsQuery(Guid SellerId, string? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<(List<ReturnRequestDto> Items, int Total)>;

public class GetSellerReturnsHandler : IRequestHandler<GetSellerReturnsQuery, (List<ReturnRequestDto>, int)>
{
    private readonly IFulfillmentDatabase _db;
    public GetSellerReturnsHandler(IFulfillmentDatabase db) => _db = db;

    public async Task<(List<ReturnRequestDto>, int)> Handle(GetSellerReturnsQuery q, CancellationToken ct)
    {
        var query = _db.ReturnRequests.Where(r => r.SellerId == q.SellerId);
        if (!string.IsNullOrEmpty(q.Status) && Enum.TryParse<ReturnStatus>(q.Status, true, out var s))
            query = query.Where(r => r.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(r => new ReturnRequestDto(r.Id, r.OrderId, r.SellerId, r.BuyerId,
                r.Reason.ToString(), r.Description, r.Status.ToString(),
                r.RefundAmount, r.SellerNote, r.ResolvedAt, r.EvidenceImageUrls, r.CreatedAt))
            .ToListAsync(ct);
        return (items, total);
    }
}

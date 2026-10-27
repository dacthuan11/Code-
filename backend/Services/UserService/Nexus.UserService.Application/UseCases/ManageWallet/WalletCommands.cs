using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Application.Persistence;

namespace Nexus.UserService.Application.UseCases.ManageWallet;

// ── DTOs ─────────────────────────────────────────────────────────────────────
public record WalletTxDto(Guid Id, decimal Amount, string Type,
    string Description, string Reference, DateTime CreatedAt);

public record WalletDto(Guid WalletId, decimal Balance, List<WalletTxDto> RecentTransactions);

// ── Credit (cộng tiền) ────────────────────────────────────────────────────────
public record CreditWalletCommand(Guid UserId, decimal Amount, string Description, string Reference) : IRequest;

public class CreditWalletHandler : IRequestHandler<CreditWalletCommand>
{
    private readonly IUserDatabase _db;
    private readonly IEventBus _eventBus;
    public CreditWalletHandler(IUserDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(CreditWalletCommand cmd, CancellationToken ct)
    {
        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Ví không tồn tại.");
        wallet.Credit(cmd.Amount, cmd.Description, cmd.Reference);
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new WalletCreditedEvent(cmd.UserId, cmd.Amount, cmd.Reference), ct);
    }
}

// ── Debit (trừ tiền) ─────────────────────────────────────────────────────────
public record DebitWalletCommand(Guid UserId, decimal Amount, string Description, string Reference) : IRequest;

public class DebitWalletHandler : IRequestHandler<DebitWalletCommand>
{
    private readonly IUserDatabase _db;
    private readonly IEventBus _eventBus;
    public DebitWalletHandler(IUserDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(DebitWalletCommand cmd, CancellationToken ct)
    {
        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Ví không tồn tại.");
        wallet.Debit(cmd.Amount, cmd.Description, cmd.Reference);
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new WalletDebitedEvent(cmd.UserId, cmd.Amount, cmd.Reference), ct);
    }
}

// ── Get Wallet ────────────────────────────────────────────────────────────────
public record GetWalletQuery(Guid UserId) : IRequest<WalletDto>;

public class GetWalletHandler : IRequestHandler<GetWalletQuery, WalletDto>
{
    private readonly IUserDatabase _db;
    public GetWalletHandler(IUserDatabase db) => _db = db;

    public async Task<WalletDto> Handle(GetWalletQuery q, CancellationToken ct)
    {
        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == q.UserId, ct)
            ?? throw new KeyNotFoundException("Ví không tồn tại.");

        var recent = wallet.Transactions
            .OrderByDescending(t => t.CreatedAt).Take(20)
            .Select(t => new WalletTxDto(t.Id, t.Amount, t.Type.ToString(),
                t.Description, t.Reference, t.CreatedAt))
            .ToList();

        return new WalletDto(wallet.Id, wallet.Balance, recent);
    }
}

// ── Get History ───────────────────────────────────────────────────────────────
public record GetWalletHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<(List<WalletTxDto> Items, int Total)>;

public class GetWalletHistoryHandler : IRequestHandler<GetWalletHistoryQuery, (List<WalletTxDto>, int)>
{
    private readonly IUserDatabase _db;
    public GetWalletHistoryHandler(IUserDatabase db) => _db = db;

    public async Task<(List<WalletTxDto>, int)> Handle(GetWalletHistoryQuery q, CancellationToken ct)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == q.UserId, ct)
            ?? throw new KeyNotFoundException("Ví không tồn tại.");

        var query = _db.WalletTxs.Where(t => t.WalletId == wallet.Id);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(t => new WalletTxDto(t.Id, t.Amount, t.Type.ToString(),
                t.Description, t.Reference, t.CreatedAt))
            .ToListAsync(ct);

        return (items, total);
    }
}

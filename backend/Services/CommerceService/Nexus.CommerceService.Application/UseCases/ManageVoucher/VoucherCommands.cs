using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Application.Persistence;

namespace Nexus.CommerceService.Application.UseCases.ManageVoucher;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record VoucherDto(
    Guid Id, string Code, string Name, string DiscountType, decimal DiscountValue,
    decimal MaxDiscount, decimal MinOrderValue, int MaxUses, int UsedCount,
    string Scope, Guid? SellerId, DateTime ValidFrom, DateTime ValidTo, bool IsActive);

// ── Create Voucher ────────────────────────────────────────────────────────────
public record CreateVoucherCommand(
    string Code, string Name, string DiscountType, decimal DiscountValue,
    decimal MaxDiscount, decimal MinOrderValue, int MaxUses, string Scope,
    DateTime ValidFrom, DateTime ValidTo, Guid? SellerId = null) : IRequest<Guid>;

public class CreateVoucherHandler : IRequestHandler<CreateVoucherCommand, Guid>
{
    private readonly ICommerceDatabase _db;
    public CreateVoucherHandler(ICommerceDatabase db) => _db = db;

    public async Task<Guid> Handle(CreateVoucherCommand cmd, CancellationToken ct)
    {
        var exists = await _db.Vouchers.AnyAsync(v => v.Code == cmd.Code.ToUpperInvariant(), ct);
        if (exists) throw new InvalidOperationException("Mã voucher đã tồn tại.");

        if (!Enum.TryParse<DiscountType>(cmd.DiscountType, true, out var dt))
            throw new InvalidOperationException("Loại giảm giá không hợp lệ.");
        if (!Enum.TryParse<VoucherScope>(cmd.Scope, true, out var scope))
            throw new InvalidOperationException("Phạm vi voucher không hợp lệ.");

        var voucher = Voucher.Create(cmd.Code, cmd.Name, dt, cmd.DiscountValue,
            cmd.MaxDiscount, cmd.MinOrderValue, cmd.MaxUses, scope,
            cmd.ValidFrom, cmd.ValidTo, cmd.SellerId);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(ct);
        return voucher.Id;
    }
}

// ── Validate / Get Voucher ────────────────────────────────────────────────────
public record ValidateVoucherQuery(string Code, decimal OrderTotal, Guid? SellerId = null)
    : IRequest<VoucherValidationResult>;

public record VoucherValidationResult(bool IsValid, string? Message, decimal DiscountAmount, VoucherDto? Voucher);

public class ValidateVoucherHandler : IRequestHandler<ValidateVoucherQuery, VoucherValidationResult>
{
    private readonly ICommerceDatabase _db;
    public ValidateVoucherHandler(ICommerceDatabase db) => _db = db;

    public async Task<VoucherValidationResult> Handle(ValidateVoucherQuery q, CancellationToken ct)
    {
        var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Code == q.Code.ToUpperInvariant(), ct);
        if (v is null) return new(false, "Mã voucher không tồn tại.", 0, null);
        if (!v.IsActive) return new(false, "Voucher đã bị vô hiệu hóa.", 0, null);
        if (v.Scope == VoucherScope.Shop && v.SellerId != q.SellerId)
            return new(false, "Voucher không áp dụng cho shop này.", 0, null);
        if (!v.CanUse(q.OrderTotal)) return new(false, "Voucher không hợp lệ hoặc đã hết lượt.", 0, null);
        var discount = v.CalculateDiscount(q.OrderTotal);
        return new(true, null, discount, ToDto(v));
    }

    private static VoucherDto ToDto(Voucher v) => new(v.Id, v.Code, v.Name,
        v.DiscountType.ToString(), v.DiscountValue, v.MaxDiscount, v.MinOrderValue,
        v.MaxUses, v.UsedCount, v.Scope.ToString(), v.SellerId, v.ValidFrom, v.ValidTo, v.IsActive);
}

// ── Get Available Vouchers ────────────────────────────────────────────────────
public record GetAvailableVouchersQuery(decimal OrderTotal, Guid? SellerId = null, int Page = 1, int PageSize = 20)
    : IRequest<List<VoucherDto>>;

public class GetAvailableVouchersHandler : IRequestHandler<GetAvailableVouchersQuery, List<VoucherDto>>
{
    private readonly ICommerceDatabase _db;
    public GetAvailableVouchersHandler(ICommerceDatabase db) => _db = db;

    public async Task<List<VoucherDto>> Handle(GetAvailableVouchersQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.Vouchers
            .Where(v => v.IsActive
                && v.ValidFrom <= now && v.ValidTo >= now
                && v.UsedCount < v.MaxUses
                && v.MinOrderValue <= q.OrderTotal
                && (v.Scope == VoucherScope.Platform || v.SellerId == q.SellerId))
            .OrderBy(v => v.ValidTo)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(v => new VoucherDto(v.Id, v.Code, v.Name,
                v.DiscountType.ToString(), v.DiscountValue, v.MaxDiscount, v.MinOrderValue,
                v.MaxUses, v.UsedCount, v.Scope.ToString(), v.SellerId, v.ValidFrom, v.ValidTo, v.IsActive))
            .ToListAsync(ct);
    }
}

// ── Deactivate Voucher ────────────────────────────────────────────────────────
public record DeactivateVoucherCommand(Guid VoucherId) : IRequest;

public class DeactivateVoucherHandler : IRequestHandler<DeactivateVoucherCommand>
{
    private readonly ICommerceDatabase _db;
    public DeactivateVoucherHandler(ICommerceDatabase db) => _db = db;

    public async Task Handle(DeactivateVoucherCommand cmd, CancellationToken ct)
    {
        var v = await _db.Vouchers.FindAsync(new object[] { cmd.VoucherId }, ct)
            ?? throw new KeyNotFoundException("Voucher không tồn tại.");
        v.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}

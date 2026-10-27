using Nexus.Shared.Domain;

namespace Nexus.CommerceService.Domain.Entities;

public enum DiscountType { Percent, Fixed }
public enum VoucherScope { Platform, Shop }

public class Voucher : BaseEntity
{
    public string       Code          { get; private set; } = default!;
    public string       Name          { get; private set; } = default!;
    public DiscountType DiscountType  { get; private set; }
    public decimal      DiscountValue { get; private set; }
    public decimal      MaxDiscount   { get; private set; }
    public decimal      MinOrderValue { get; private set; }
    public int          MaxUses       { get; private set; }
    public int          UsedCount     { get; private set; }
    public VoucherScope Scope         { get; private set; }
    public Guid?        SellerId      { get; private set; }
    public DateTime     ValidFrom     { get; private set; }
    public DateTime     ValidTo       { get; private set; }
    public bool         IsActive      { get; private set; }

    private Voucher() { }

    public static Voucher Create(string code, string name, DiscountType discountType,
        decimal discountValue, decimal maxDiscount, decimal minOrderValue,
        int maxUses, VoucherScope scope, DateTime validFrom, DateTime validTo,
        Guid? sellerId = null)
        => new()
        {
            Code = code.ToUpperInvariant(), Name = name, DiscountType = discountType,
            DiscountValue = discountValue, MaxDiscount = maxDiscount, MinOrderValue = minOrderValue,
            MaxUses = maxUses, Scope = scope, ValidFrom = validFrom, ValidTo = validTo,
            SellerId = sellerId, IsActive = true
        };

    public bool CanUse(decimal orderTotal)
        => IsActive && UsedCount < MaxUses
            && DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo
            && orderTotal >= MinOrderValue;

    public decimal CalculateDiscount(decimal orderTotal)
    {
        if (!CanUse(orderTotal)) return 0;
        return DiscountType == DiscountType.Percent
            ? Math.Min(orderTotal * DiscountValue / 100, MaxDiscount)
            : Math.Min(DiscountValue, orderTotal);
    }

    public void Use()
    {
        if (UsedCount < MaxUses) { UsedCount++; Touch(); }
    }

    public void Deactivate() { IsActive = false; Touch(); }
}

public class VoucherUsage : BaseEntity
{
    public Guid    VoucherId      { get; private set; }
    public Guid    UserId         { get; private set; }
    public Guid    OrderId        { get; private set; }
    public decimal DiscountAmount { get; private set; }

    private VoucherUsage() { }

    public static VoucherUsage Create(Guid voucherId, Guid userId, Guid orderId, decimal discountAmount)
        => new() { VoucherId = voucherId, UserId = userId, OrderId = orderId, DiscountAmount = discountAmount };
}

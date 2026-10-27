using Nexus.Shared.Domain;

namespace Nexus.AuctionService.Domain.Entities;

public enum FlashSaleStatus { Draft, Active, Ended, Cancelled }

public class FlashSale : BaseEntity
{
    public string          Name      { get; private set; } = default!;
    public DateTime        StartTime { get; private set; }
    public DateTime        EndTime   { get; private set; }
    public FlashSaleStatus Status    { get; private set; }
    public Guid            SellerId  { get; private set; }

    private readonly List<FlashSaleItem> _items = new();
    public IReadOnlyList<FlashSaleItem> Items => _items.AsReadOnly();

    private FlashSale() { }

    public static FlashSale Create(string name, DateTime startTime, DateTime endTime, Guid sellerId)
    {
        if (endTime <= startTime)
            throw new InvalidOperationException("Thời gian kết thúc phải sau thời gian bắt đầu.");
        return new()
        {
            Name = name, StartTime = startTime, EndTime = endTime,
            SellerId = sellerId, Status = FlashSaleStatus.Draft
        };
    }

    public void AddItem(FlashSaleItem item) { _items.Add(item); Touch(); }

    public void Activate()
    {
        if (Status != FlashSaleStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể kích hoạt flash sale ở trạng thái Draft.");
        if (!_items.Any())
            throw new InvalidOperationException("Flash sale phải có ít nhất 1 sản phẩm.");
        Status = FlashSaleStatus.Active;
        Touch();
    }

    public void End()
    {
        if (Status != FlashSaleStatus.Active) return;
        Status = FlashSaleStatus.Ended;
        Touch();
    }

    public bool IsActive => Status == FlashSaleStatus.Active
        && DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
}

public class FlashSaleItem : BaseEntity
{
    public Guid    FlashSaleId    { get; private set; }
    public Guid    ProductId      { get; private set; }
    public Guid    SkuId          { get; private set; }
    public decimal OriginalPrice  { get; private set; }
    public decimal FlashSalePrice { get; private set; }
    public int     TotalSlots     { get; private set; }
    public int     SoldSlots      { get; private set; }

    public int  RemainingSlots => TotalSlots - SoldSlots;
    public bool HasSlots       => RemainingSlots > 0;

    private FlashSaleItem() { }

    public static FlashSaleItem Create(Guid flashSaleId, Guid productId, Guid skuId,
        decimal originalPrice, decimal flashSalePrice, int totalSlots)
    {
        if (flashSalePrice >= originalPrice)
            throw new InvalidOperationException("Giá flash sale phải thấp hơn giá gốc.");
        if (totalSlots <= 0)
            throw new InvalidOperationException("Số lượng slot phải lớn hơn 0.");
        return new()
        {
            FlashSaleId = flashSaleId, ProductId = productId, SkuId = skuId,
            OriginalPrice = originalPrice, FlashSalePrice = flashSalePrice, TotalSlots = totalSlots
        };
    }

    public bool TryReserve()
    {
        if (!HasSlots) return false;
        SoldSlots++;
        Touch();
        return true;
    }
}

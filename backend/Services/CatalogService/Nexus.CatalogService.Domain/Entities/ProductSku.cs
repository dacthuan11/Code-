using Nexus.Shared.Domain;

namespace Nexus.CatalogService.Domain.Entities;

public class ProductSku : BaseEntity
{
    public Guid   ProductId { get; private set; }
    public string SkuCode   { get; private set; } = default!;  // VD: "RED-XL"
    public decimal Price    { get; private set; }
    public List<SkuAttribute> Attributes { get; private set; } = new();

    private ProductSku() { }

    public static ProductSku Create(Guid productId, string skuCode, decimal price,
        List<SkuAttribute> attributes) => new()
    {
        ProductId = productId,
        SkuCode   = skuCode,
        Price     = price,
        Attributes = attributes
    };

    public void UpdatePrice(decimal newPrice) { Price = newPrice; Touch(); }
    public void UpdateAttributes(List<SkuAttribute> attrs) { Attributes = attrs; Touch(); }
}

public class SkuAttribute : BaseEntity
{
    public Guid   SkuId { get; private set; }
    public string Name  { get; private set; } = default!;  // "Màu", "Size", "Chất liệu"
    public string Value { get; private set; } = default!;  // "Đỏ", "XL", "Cotton"

    private SkuAttribute() { }

    public static SkuAttribute Create(Guid skuId, string name, string value) =>
        new() { SkuId = skuId, Name = name, Value = value };
}

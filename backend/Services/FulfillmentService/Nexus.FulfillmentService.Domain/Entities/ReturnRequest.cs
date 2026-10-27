using Nexus.Shared.Domain;

namespace Nexus.FulfillmentService.Domain.Entities;

public enum ReturnStatus { Pending, Approved, Rejected, Completed }
public enum ReturnReason { WrongItem, Defective, NotAsDescribed, Other }

public class ReturnRequest : BaseEntity
{
    public Guid OrderId      { get; private set; }
    public Guid SellerId     { get; private set; }
    public Guid BuyerId      { get; private set; }
    public ReturnReason Reason  { get; private set; }
    public string Description   { get; private set; } = default!;
    public List<string> EvidenceImageUrls { get; private set; } = new();
    public ReturnStatus Status  { get; private set; }
    public decimal RefundAmount { get; private set; }
    public string? SellerNote   { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private ReturnRequest() { }

    public static ReturnRequest Create(Guid orderId, Guid sellerId, Guid buyerId,
        ReturnReason reason, string description, decimal refundAmount, List<string>? evidenceUrls = null)
        => new()
        {
            OrderId = orderId, SellerId = sellerId, BuyerId = buyerId,
            Reason = reason, Description = description, RefundAmount = refundAmount,
            EvidenceImageUrls = evidenceUrls ?? new(),
            Status = ReturnStatus.Pending
        };

    public void Approve(string? note = null)
    {
        if (Status != ReturnStatus.Pending) throw new InvalidOperationException("Chỉ có thể duyệt yêu cầu đang chờ.");
        Status = ReturnStatus.Approved;
        SellerNote = note;
        ResolvedAt = DateTime.UtcNow;
        Touch();
    }

    public void Reject(string reason)
    {
        if (Status != ReturnStatus.Pending) throw new InvalidOperationException("Chỉ có thể từ chối yêu cầu đang chờ.");
        Status = ReturnStatus.Rejected;
        SellerNote = reason;
        ResolvedAt = DateTime.UtcNow;
        Touch();
    }

    public void Complete()
    {
        if (Status != ReturnStatus.Approved) throw new InvalidOperationException("Chỉ hoàn thành khi đã approved.");
        Status = ReturnStatus.Completed;
        Touch();
    }
}

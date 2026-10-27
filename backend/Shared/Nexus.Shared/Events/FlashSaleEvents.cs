using Nexus.Shared.Messaging;

namespace Nexus.Shared.Events;

// ===== Flash Sale Events =====
// Publish từ AuctionService — NotificationService gửi thông báo, FE cập nhật real-time

public record FlashSaleStartedEvent(Guid FlashSaleId, string Name, DateTime EndTime, List<Guid> SkuIds) : BaseIntegrationEvent;
public record FlashSaleEndedEvent(Guid FlashSaleId, string Name) : BaseIntegrationEvent;
public record FlashSaleSlotRunningLowEvent(Guid FlashSaleId, Guid SkuId, int Remaining) : BaseIntegrationEvent;

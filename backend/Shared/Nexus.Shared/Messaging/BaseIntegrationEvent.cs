namespace Nexus.Shared.Messaging;

/// <summary>
/// Base record cho tất cả integration events giữa các microservice.
/// Dùng record (immutable) vì event không nên bị thay đổi sau khi tạo.
/// </summary>
public abstract record BaseIntegrationEvent
{
    // ID duy nhất của event để dedup khi consumer nhận trùng
    public Guid EventId { get; init; } = Guid.NewGuid();

    // Thời điểm event xảy ra — UTC để đồng nhất cross-timezone
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

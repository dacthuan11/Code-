using Nexus.Shared.Messaging;

namespace Nexus.Shared.Events;

// ===== User Domain Events =====
// Các event này được publish từ UserService khi có thay đổi về user
// Các service khác (Notification, Commerce...) có thể subscribe để phản ứng

// Khi user đăng ký thành công — NotificationService gửi email xác thực
public record UserRegisteredEvent(Guid UserId, string Email, string FullName, string VerificationToken) : BaseIntegrationEvent;

// Khi user cập nhật thông tin — các service cache user info cần invalidate
public record UserUpdatedEvent(Guid UserId, string FullName) : BaseIntegrationEvent;

// Soft delete — không phải xóa hẳn, chỉ mark để các service biết
public record UserDeletedEvent(Guid UserId) : BaseIntegrationEvent;

// Audit: ghi lại lịch sử đăng nhập cho security monitoring
public record UserLoggedInEvent(Guid UserId, string Email) : BaseIntegrationEvent;
public record UserLoggedOutEvent(Guid UserId) : BaseIntegrationEvent;

// Security events — có thể trigger notification "someone changed your password"
public record UserPasswordChangedEvent(Guid UserId) : BaseIntegrationEvent;
public record UserPasswordResetEvent(Guid UserId, string Email) : BaseIntegrationEvent;

// Khi admin assign role mới cho user
public record UserRoleAssignedEvent(Guid UserId, string RoleName) : BaseIntegrationEvent;

// Hệ thống reputation — buyer/seller đánh giá nhau sau giao dịch
public record UserRatedEvent(Guid RaterId, Guid RateeId, int Score, string? Comment) : BaseIntegrationEvent;

// Cập nhật điểm uy tín sau mỗi transaction
public record ReputationScoreUpdatedEvent(Guid UserId, decimal NewScore, string TrustLevel) : BaseIntegrationEvent;

// Phạt điểm khi vi phạm (không thanh toán auction, hủy đơn nhiều lần...)
public record ReputationPenaltyAppliedEvent(Guid UserId, decimal Penalty, string Reason) : BaseIntegrationEvent;

public record SellerRegisteredEvent(Guid UserId, string ShopName, string ShopSlug) : BaseIntegrationEvent;
public record WalletCreditedEvent(Guid UserId, decimal Amount, string Reference) : BaseIntegrationEvent;
public record WalletDebitedEvent(Guid UserId, decimal Amount, string Reference) : BaseIntegrationEvent;

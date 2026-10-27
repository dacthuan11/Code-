using Microsoft.Extensions.Logging;
using Nexus.CommerceService.Application.UseCases.Checkout;
using Nexus.CommerceService.Domain.Entities;

namespace Nexus.CommerceService.Infrastructure.Adapters;

// StubPaymentGateway - fake payment gateway dùng cho DEV và testing.
// Không gọi Stripe hay bất kỳ external service nào - luôn trả về thành công.
//
// Lý do cần Stub thay vì mock trong test:
//   - Dev local không cần Stripe account
//   - Integration test chạy không cần network
//   - Demo dễ dàng hơn
//
// Khi ready production: thay bằng StripePaymentGateway trong DI container (Program.cs)
// mà không cần sửa bất kỳ handler nào (nhờ interface IPaymentGateway)
public class StubPaymentGateway : IPaymentGateway
{
    private readonly ILogger<StubPaymentGateway> _logger;
    public StubPaymentGateway(ILogger<StubPaymentGateway> logger) => _logger = logger;

    // Trả về một URL giả - FE sẽ redirect đến đây thay vì Stripe checkout
    // Port 9999 để không conflict với các service khác (5001-5006)
    public Task<string> CreatePaymentSessionAsync(Order order, CancellationToken ct)
    {
        _logger.LogInformation("[StubPayment] Created payment session for order {OrderId} amount {Amount}", order.Id, order.TotalAmount);
        return Task.FromResult($"http://localhost:9999/pay/stub?orderId={order.Id}");
    }

    // Luôn trả về true - stub không verify gì cả
    // Real implementation sẽ gọi Stripe API để verify payment intent
    public Task<bool> VerifyPaymentAsync(string paymentIntentId, CancellationToken ct)
    {
        _logger.LogInformation("[StubPayment] Verified payment {PaymentId}", paymentIntentId);
        return Task.FromResult(true);
    }

    // Luôn trả về true - refund luôn "thành công" trong môi trường dev
    public Task<bool> RefundAsync(string paymentIntentId, decimal amount, CancellationToken ct)
    {
        _logger.LogInformation("[StubPayment] Refunded {Amount} for {PaymentId}", amount, paymentIntentId);
        return Task.FromResult(true);
    }
}

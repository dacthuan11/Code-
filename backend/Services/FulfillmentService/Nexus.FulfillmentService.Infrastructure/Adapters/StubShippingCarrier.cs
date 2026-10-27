using Microsoft.Extensions.Logging;
using Nexus.FulfillmentService.Application.UseCases.CreateShipment;

namespace Nexus.FulfillmentService.Infrastructure.Adapters;

/// <summary>
/// Stub implementation của IShippingCarrier - dùng để dev/test mà không cần GHN API thật.
/// Trả về tracking number giả và phí ship cố định 25.000 VNĐ.
/// Khi deploy production: thay bằng GhnShippingCarrier thật (đăng ký DI trong Program.cs).
/// </summary>
public class StubShippingCarrier : IShippingCarrier
{
    private readonly ILogger<StubShippingCarrier> _logger;
    public StubShippingCarrier(ILogger<StubShippingCarrier> logger) => _logger = logger;

    public Task<(string TrackingNumber, decimal Fee)> CreateShipmentAsync(CreateShipmentCommand cmd, CancellationToken ct)
    {
        // Generate tracking number fake với prefix GHN-STUB để dễ phân biệt với tracking thật
        var tracking = $"GHN-STUB-{Guid.NewGuid():N}"[..17];
        _logger.LogInformation("[StubGHN] Created shipment {Tracking} for order {OrderId}", tracking, cmd.OrderId);

        // Phí cố định 25k - không quan tâm địa chỉ hay DeclaredValue
        return Task.FromResult((tracking, 25_000m));
    }

    public Task<string> GetTrackingStatusAsync(string trackingNumber, CancellationToken ct)
    {
        _logger.LogInformation("[StubGHN] Tracking status for {Tracking}", trackingNumber);
        return Task.FromResult("in_transit");
    }

    public Task<ShippingFeeEstimate> CalculateFeeAsync(ShippingFeeRequest req, CancellationToken ct)
    {
        // Phí stub cố định: 25k Standard
        return Task.FromResult(new ShippingFeeEstimate(25_000, 23_000, 2_000, "Standard"));
    }
}

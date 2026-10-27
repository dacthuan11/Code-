using Microsoft.Extensions.Options;
using Nexus.FulfillmentService.Application.UseCases.CreateShipment;

namespace Nexus.FulfillmentService.Infrastructure.Adapters.Ghn;


/// <summary>
/// Implementation thật của IShippingCarrier dùng GHN API.
/// Để kích hoạt: trong Program.cs đổi StubShippingCarrier → GhnShippingCarrier.
/// </summary>
public class GhnShippingCarrier : IShippingCarrier
{
    private readonly GhnClient _ghn;
    private readonly GhnSettings _cfg;

    public GhnShippingCarrier(GhnClient ghn, IOptions<GhnSettings> cfg)
    {
        _ghn = ghn;
        _cfg = cfg.Value;
    }

    public async Task<(string TrackingNumber, decimal Fee)> CreateShipmentAsync(
        CreateShipmentCommand cmd, CancellationToken ct)
    {
        var req = new GhnCreateOrderRequest
        {
            ToName      = cmd.RecipientName,
            ToPhone     = cmd.Phone,
            ToAddress   = cmd.Address,
            ToWardCode  = cmd.ToWardCode,
            ToDistrictId = cmd.ToDistrictId,
            InsuranceValue = (long)cmd.DeclaredValue,
            CodAmount   = 0, // Mặc định đã thanh toán online; COD thì truyền DeclaredValue
            Items       = [new GhnItem { Name = "Sản phẩm", Quantity = 1, Price = (long)cmd.DeclaredValue }]
        };

        var order = await _ghn.CreateOrderAsync(req, ct);
        return (order.OrderCode, order.TotalFee);
    }

    public async Task<string> GetTrackingStatusAsync(string trackingNumber, CancellationToken ct)
        => await _ghn.GetOrderStatusAsync(trackingNumber, ct);

    public async Task<ShippingFeeEstimate> CalculateFeeAsync(ShippingFeeRequest req, CancellationToken ct)
    {
        var ghnReq = new GhnFeeRequest(
            ServiceTypeId:  _cfg.DefaultServiceTypeId,
            FromDistrictId: _cfg.FromDistrictId,
            ToDistrictId:   req.ToDistrictId,
            ToWardCode:     req.ToWardCode,
            Weight:         req.Weight,
            Length:         req.Length,
            Width:          req.Width,
            Height:         req.Height,
            InsuranceValue: req.InsuranceValue
        );
        var fee = await _ghn.CalculateFeeAsync(ghnReq, ct);
        var label = _cfg.DefaultServiceTypeId switch { 1 => "Express", 3 => "Saving", _ => "Standard" };
        return new ShippingFeeEstimate(fee.Total, fee.ServiceFee, fee.InsuranceFee, label);
    }
}

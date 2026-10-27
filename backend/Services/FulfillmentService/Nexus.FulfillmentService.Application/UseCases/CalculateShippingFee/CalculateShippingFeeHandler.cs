using MediatR;
using Nexus.FulfillmentService.Application.UseCases.CreateShipment;

namespace Nexus.FulfillmentService.Application.UseCases.CalculateShippingFee;

public class CalculateShippingFeeHandler : IRequestHandler<CalculateShippingFeeQuery, ShippingFeeResult>
{
    private readonly IShippingCarrier _carrier;
    public CalculateShippingFeeHandler(IShippingCarrier carrier) => _carrier = carrier;

    public async Task<ShippingFeeResult> Handle(CalculateShippingFeeQuery q, CancellationToken ct)
    {
        var req = new ShippingFeeRequest(q.ToDistrictId, q.ToWardCode, q.InsuranceValue,
            q.Weight, q.Length, q.Width, q.Height);
        var estimate = await _carrier.CalculateFeeAsync(req, ct);
        return new ShippingFeeResult(estimate.TotalFee, estimate.ServiceFee, estimate.InsuranceFee, estimate.ServiceType);
    }
}

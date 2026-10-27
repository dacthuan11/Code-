using MediatR;

namespace Nexus.FulfillmentService.Application.UseCases.CalculateShippingFee;

public record CalculateShippingFeeQuery(
    int ToDistrictId,
    string ToWardCode,
    long InsuranceValue,  // = giá trị đơn hàng (VNĐ)
    int Weight = 500,     // gram
    int Length = 20, int Width = 15, int Height = 10
) : IRequest<ShippingFeeResult>;

public record ShippingFeeResult(
    long TotalFee,
    long ServiceFee,
    long InsuranceFee,
    string ServiceType   // "Standard" | "Express"
);

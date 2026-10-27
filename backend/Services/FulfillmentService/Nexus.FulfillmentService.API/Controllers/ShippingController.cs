using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.FulfillmentService.Application.UseCases.CalculateShippingFee;
using Nexus.FulfillmentService.Infrastructure.Adapters.Ghn;

namespace Nexus.FulfillmentService.API.Controllers;

/// <summary>
/// Endpoint công khai (không cần đăng nhập) cho:
/// - Lấy danh sách tỉnh/quận/phường từ GHN (dùng cho dropdown địa chỉ ở checkout)
/// - Tính phí vận chuyển trước khi đặt hàng
/// </summary>
[ApiController]
[Route("api/v1/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly GhnClient _ghn;

    public ShippingController(IMediator mediator, GhnClient ghn)
    {
        _mediator = mediator;
        _ghn = ghn;
    }

    // ── Địa chỉ GHN ─────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/shipping/provinces — Danh sách 63 tỉnh/thành</summary>
    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces(CancellationToken ct)
    {
        var data = await _ghn.GetProvincesAsync(ct);
        return Ok(data.Select(p => new { p.ProvinceId, p.ProvinceName }));
    }

    /// <summary>GET /api/v1/shipping/districts?provinceId=202 — Quận/huyện theo tỉnh</summary>
    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] int provinceId, CancellationToken ct)
    {
        var data = await _ghn.GetDistrictsAsync(provinceId, ct);
        return Ok(data.Select(d => new { d.DistrictId, d.DistrictName, d.ProvinceId }));
    }

    /// <summary>GET /api/v1/shipping/wards?districtId=1442 — Phường/xã theo quận</summary>
    [HttpGet("wards")]
    public async Task<IActionResult> GetWards([FromQuery] int districtId, CancellationToken ct)
    {
        var data = await _ghn.GetWardsAsync(districtId, ct);
        return Ok(data.Select(w => new { w.WardCode, w.WardName, w.DistrictId }));
    }

    // ── Tính phí ─────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/v1/shipping/fee — Tính phí ship
    /// Body: { toDistrictId, toWardCode, insuranceValue, weight? }
    /// </summary>
    [HttpPost("fee")]
    public async Task<IActionResult> CalculateFee([FromBody] CalculateShippingFeeQuery query, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

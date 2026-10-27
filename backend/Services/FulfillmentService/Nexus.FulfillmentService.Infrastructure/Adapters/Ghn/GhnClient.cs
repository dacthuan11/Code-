using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexus.FulfillmentService.Infrastructure.Adapters.Ghn;

/// <summary>
/// HTTP client wrapper gọi GHN API.
/// Staging:    dev-online-gateway.ghn.vn
/// Production: online-gateway.ghn.vn
/// Chỉ đổi BaseUrl trong config là chạy được production.
/// </summary>
public class GhnClient
{
    private readonly HttpClient _http;
    private readonly GhnSettings _cfg;
    private readonly ILogger<GhnClient> _log;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public GhnClient(HttpClient http, IOptions<GhnSettings> cfg, ILogger<GhnClient> log)
    {
        _http = http;
        _cfg = cfg.Value;
        _log = log;
        _http.BaseAddress = new Uri(_cfg.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add("Token", _cfg.Token);
    }

    // ── Master data ──────────────────────────────────────────────────────────

    public async Task<List<GhnProvince>> GetProvincesAsync(CancellationToken ct = default)
    {
        var res = await _http.GetFromJsonAsync<GhnResponse<List<GhnProvince>>>("master-data/province", _json, ct);
        return res?.Data ?? [];
    }

    public async Task<List<GhnDistrict>> GetDistrictsAsync(int provinceId, CancellationToken ct = default)
    {
        var res = await _http.GetFromJsonAsync<GhnResponse<List<GhnDistrict>>>(
            $"master-data/district?province_id={provinceId}", _json, ct);
        return res?.Data ?? [];
    }

    public async Task<List<GhnWard>> GetWardsAsync(int districtId, CancellationToken ct = default)
    {
        var res = await _http.GetFromJsonAsync<GhnResponse<List<GhnWard>>>(
            $"master-data/ward?district_id={districtId}", _json, ct);
        return res?.Data ?? [];
    }

    // ── Fee calculation ──────────────────────────────────────────────────────

    public async Task<GhnFeeResponse> CalculateFeeAsync(GhnFeeRequest req, CancellationToken ct = default)
    {
        // ShopId phải đi trong header cho fee calculation
        var request = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/fee");
        request.Headers.Add("ShopId", _cfg.ShopId.ToString());
        request.Content = JsonContent.Create(req, options: _json);

        var resp = await _http.SendAsync(request, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<GhnResponse<GhnFeeResponse>>(_json, ct);
        return body?.Data ?? throw new InvalidOperationException("GHN fee response empty");
    }

    // ── Create order ─────────────────────────────────────────────────────────

    public async Task<GhnOrderResponse> CreateOrderAsync(GhnCreateOrderRequest req, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/create");
        request.Headers.Add("ShopId", _cfg.ShopId.ToString());
        request.Content = JsonContent.Create(req, options: _json);

        var resp = await _http.SendAsync(request, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogError("GHN create order failed: {Status} {Body}", resp.StatusCode, body);
            throw new InvalidOperationException($"GHN create order failed: {body}");
        }

        var result = JsonSerializer.Deserialize<GhnResponse<GhnOrderResponse>>(body, _json);
        return result?.Data ?? throw new InvalidOperationException("GHN order response empty");
    }

    // ── Track order ──────────────────────────────────────────────────────────

    public async Task<string> GetOrderStatusAsync(string orderCode, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/detail");
        req.Headers.Add("ShopId", _cfg.ShopId.ToString());
        req.Content = JsonContent.Create(new { order_code = orderCode }, options: _json);

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<GhnResponse<GhnOrderDetail>>(_json, ct);
        return body?.Data?.Status ?? "unknown";
    }
}

// ── DTO models ───────────────────────────────────────────────────────────────

public record GhnResponse<T>(int Code, string? Message, T? Data);

public record GhnProvince(
    [property: JsonPropertyName("ProvinceID")] int ProvinceId,
    [property: JsonPropertyName("ProvinceName")] string ProvinceName);

public record GhnDistrict(
    [property: JsonPropertyName("DistrictID")] int DistrictId,
    [property: JsonPropertyName("DistrictName")] string DistrictName,
    [property: JsonPropertyName("ProvinceID")] int ProvinceId);

public record GhnWard(
    [property: JsonPropertyName("WardCode")] string WardCode,
    [property: JsonPropertyName("WardName")] string WardName,
    [property: JsonPropertyName("DistrictID")] int DistrictId);

public record GhnFeeRequest(
    [property: JsonPropertyName("service_type_id")] int ServiceTypeId,
    [property: JsonPropertyName("from_district_id")] int FromDistrictId,
    [property: JsonPropertyName("to_district_id")] int ToDistrictId,
    [property: JsonPropertyName("to_ward_code")] string ToWardCode,
    [property: JsonPropertyName("weight")] int Weight,            // gram
    [property: JsonPropertyName("length")] int Length,            // cm
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("insurance_value")] long InsuranceValue);

public record GhnFeeResponse(
    [property: JsonPropertyName("total")] long Total,
    [property: JsonPropertyName("service_fee")] long ServiceFee,
    [property: JsonPropertyName("insurance_fee")] long InsuranceFee,
    [property: JsonPropertyName("coupon_value")] long CouponValue);

public class GhnCreateOrderRequest
{
    [JsonPropertyName("payment_type_id")] public int PaymentTypeId { get; set; } = 2; // 2 = người mua trả phí
    [JsonPropertyName("required_note")] public string RequiredNote { get; set; } = "CHOXEMHANGKHONGTHU";
    [JsonPropertyName("to_name")] public string ToName { get; set; } = default!;
    [JsonPropertyName("to_phone")] public string ToPhone { get; set; } = default!;
    [JsonPropertyName("to_address")] public string ToAddress { get; set; } = default!;
    [JsonPropertyName("to_ward_code")] public string ToWardCode { get; set; } = default!;
    [JsonPropertyName("to_district_id")] public int ToDistrictId { get; set; }
    [JsonPropertyName("cod_amount")] public long CodAmount { get; set; } // 0 nếu đã thanh toán online
    [JsonPropertyName("weight")] public int Weight { get; set; } = 500;   // gram, mặc định 500g
    [JsonPropertyName("length")] public int Length { get; set; } = 20;
    [JsonPropertyName("width")] public int Width { get; set; } = 15;
    [JsonPropertyName("height")] public int Height { get; set; } = 10;
    [JsonPropertyName("service_type_id")] public int ServiceTypeId { get; set; } = 2;
    [JsonPropertyName("insurance_value")] public long InsuranceValue { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("items")] public List<GhnItem> Items { get; set; } = [];
}

public class GhnItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = default!;
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("price")] public long Price { get; set; }
}

public record GhnOrderResponse(
    [property: JsonPropertyName("order_code")] string OrderCode,
    [property: JsonPropertyName("total_fee")] long TotalFee,
    [property: JsonPropertyName("expected_delivery_time")] string? ExpectedDeliveryTime);

public record GhnOrderDetail(
    [property: JsonPropertyName("order_code")] string OrderCode,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("updated_date")] string? UpdatedDate);

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Nexus.CommerceService.Infrastructure.Payment;

public interface IVnpayGateway
{
    string BuildPaymentUrl(Guid orderId, decimal amount, string orderInfo, string returnUrl, string ipAddress);
    bool ValidateCallback(Dictionary<string, string> queryParams);
    string GetTransactionStatus(Dictionary<string, string> queryParams);
}

public class VnpayPaymentGateway : IVnpayGateway
{
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _baseUrl;

    public VnpayPaymentGateway(IConfiguration config)
    {
        _tmnCode   = config["Vnpay:TmnCode"]   ?? "DEMO_TMN";
        _hashSecret = config["Vnpay:HashSecret"] ?? "DEMO_SECRET_KEY_32CHARS_MINIMUM_";
        _baseUrl   = config["Vnpay:BaseUrl"]   ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    }

    public string BuildPaymentUrl(Guid orderId, decimal amount, string orderInfo, string returnUrl, string ipAddress)
    {
        var vnpParams = new SortedDictionary<string, string>
        {
            ["vnp_Version"]    = "2.1.0",
            ["vnp_Command"]    = "pay",
            ["vnp_TmnCode"]    = _tmnCode,
            ["vnp_Amount"]     = ((long)(amount * 100)).ToString(),
            ["vnp_CurrCode"]   = "VND",
            ["vnp_TxnRef"]     = orderId.ToString("N")[..15],
            ["vnp_OrderInfo"]  = orderInfo,
            ["vnp_OrderType"]  = "other",
            ["vnp_Locale"]     = "vn",
            ["vnp_ReturnUrl"]  = returnUrl,
            ["vnp_IpAddr"]     = ipAddress,
            ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = DateTime.UtcNow.AddHours(7).AddMinutes(30).ToString("yyyyMMddHHmmss"),
        };

        var queryString = string.Join("&", vnpParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var secureHash = HmacSha512(_hashSecret, queryString);
        return $"{_baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    public bool ValidateCallback(Dictionary<string, string> queryParams)
    {
        if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash)) return false;
        var filtered = new SortedDictionary<string, string>(
            queryParams
                .Where(kv => kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash")
                .ToDictionary(k => k.Key, v => v.Value));
        var queryString = string.Join("&", filtered.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var expected = HmacSha512(_hashSecret, queryString);
        return string.Equals(receivedHash, expected, StringComparison.OrdinalIgnoreCase);
    }

    public string GetTransactionStatus(Dictionary<string, string> queryParams)
        => queryParams.TryGetValue("vnp_ResponseCode", out var code) && code == "00"
            ? "Success" : "Failed";

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }
}

namespace Nexus.NotificationService.Application.Commons;

/// <summary>
/// Template email Nexus — phong cách "Command Deck" tối màu, adapted từ SEAL Email Template.
/// Bảng màu: Deep Space Navy (#0a0f1d) nền, Electric Mint (#00d4aa) accent, Gold Amber (#f59e0b) secondary.
/// </summary>
public static class NexusEmailTemplate
{
    public enum Callout { Info, Success, Danger, Warning }

    private const string BgBase       = "#0a0f1d";
    private const string BgPanel      = "#111a2e";
    private const string BgInput      = "#17243e";
    private const string AccentMint   = "#00d4aa";
    private const string AccentGold   = "#f59e0b";
    private const string ColorDanger  = "#f87171";
    private const string TextPrimary  = "#f1f5f9";
    private const string TextMuted    = "#94a3b8";
    private const string BorderMuted  = "#24344d";

    /// <summary>
    /// Render email hoàn chỉnh.
    /// </summary>
    public static string Render(
        string heading,
        string? greetingName,
        string introHtml,
        string? calloutLabel = null,
        string? calloutHtml  = null,
        Callout calloutKind  = Callout.Success,
        string? ctaText      = null,
        string? ctaUrl       = null,
        string? ctaText2     = null,
        string? ctaUrl2      = null,
        string? noteHtml     = null,
        string? orderTableHtml = null)
    {
        var (cBg, cBar, cLabel) = calloutKind switch
        {
            Callout.Danger  => ("#3d1f24", ColorDanger, "#fca5a5"),
            Callout.Warning => ("#3a2c14", AccentGold,  "#fcd34d"),
            Callout.Info    => ("#1a2540", "#60a5fa",   "#93c5fd"),
            _               => ("#0f2e29", AccentMint,  "#5eead4"),
        };

        var b = new System.Text.StringBuilder();

        // Wrapper ngoài
        b.Append($@"<div style=""margin:0;padding:32px 12px;background:{BgBase};font-family:'IBM Plex Sans',Arial,Helvetica,sans-serif;"">");
        b.Append($@"<div style=""max-width:580px;margin:0 auto;background:{BgPanel};border:1px solid {BorderMuted};border-radius:14px;overflow:hidden;"">");

        // Dải gradient trên cùng (mint → gold)
        b.Append($@"<div style=""height:4px;line-height:4px;font-size:0;background:linear-gradient(90deg,{AccentMint} 0%,{AccentGold} 100%);"">&nbsp;</div>");
        b.Append(@"<div style=""padding:36px 40px 8px;"">");

        // Logo
        b.Append(@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:26px;""><tr>");
        b.Append($@"<td style=""width:46px;vertical-align:middle;""><div style=""width:46px;height:46px;background:linear-gradient(135deg,{AccentMint},#0099aa);border-radius:10px;text-align:center;line-height:46px;color:{BgBase};font-weight:900;font-size:22px;font-family:'Chakra Petch',Arial,sans-serif;"">N</div></td>");
        b.Append($@"<td style=""padding-left:12px;vertical-align:middle;""><div style=""font-size:17px;font-weight:bold;color:{TextPrimary};letter-spacing:.5px;line-height:1.1;font-family:'Chakra Petch',Arial,sans-serif;"">Nexus</div><div style=""font-size:11px;color:{TextMuted};letter-spacing:.14em;text-transform:uppercase;"">Commerce Platform</div></td>");
        b.Append(@"</tr></table>");

        // Tiêu đề + chào
        b.Append($@"<h1 style=""font-size:22px;line-height:1.3;color:{TextPrimary};margin:0 0 16px;font-weight:bold;font-family:'Chakra Petch',Arial,sans-serif;"">{heading}</h1>");
        if (!string.IsNullOrEmpty(greetingName))
            b.Append($@"<p style=""font-size:15px;line-height:1.65;margin:0 0 10px;color:{TextPrimary};"">Chào <b>{greetingName}</b>,</p>");
        b.Append($@"<p style=""font-size:15px;line-height:1.65;margin:0 0 4px;color:{TextPrimary};"">{introHtml}</p>");

        // Bảng chi tiết đơn hàng (nếu có)
        if (!string.IsNullOrEmpty(orderTableHtml))
            b.Append(orderTableHtml);

        // Callout box
        if (!string.IsNullOrEmpty(calloutLabel) || !string.IsNullOrEmpty(calloutHtml))
        {
            b.Append($@"<div style=""background:{cBg};border-left:3px solid {cBar};border-radius:8px;padding:14px 18px;margin:20px 0;"">");
            if (!string.IsNullOrEmpty(calloutLabel))
                b.Append($@"<div style=""font-size:11px;font-weight:bold;letter-spacing:.06em;text-transform:uppercase;color:{cLabel};margin-bottom:6px;"">{calloutLabel}</div>");
            if (!string.IsNullOrEmpty(calloutHtml))
                b.Append($@"<div style=""font-size:14px;line-height:1.8;color:{TextPrimary};"">{calloutHtml}</div>");
            b.Append("</div>");
        }

        // Nút CTA
        if (!string.IsNullOrEmpty(ctaText) && !string.IsNullOrEmpty(ctaUrl))
        {
            b.Append(@"<div style=""text-align:center;margin:28px 0 6px;"">");
            b.Append($@"<a href=""{ctaUrl}"" style=""display:inline-block;background:{AccentMint};color:{BgBase};text-decoration:none;font-weight:bold;font-size:14px;padding:13px 32px;border-radius:8px;margin:4px 6px;"">{ctaText}</a>");
            if (!string.IsNullOrEmpty(ctaText2) && !string.IsNullOrEmpty(ctaUrl2))
                b.Append($@"<a href=""{ctaUrl2}"" style=""display:inline-block;background:transparent;color:{ColorDanger};text-decoration:none;font-weight:bold;font-size:14px;padding:11px 32px;border:2px solid #6b3a3a;border-radius:8px;margin:4px 6px;"">{ctaText2}</a>");
            b.Append("</div>");
        }

        // Ghi chú
        if (!string.IsNullOrEmpty(noteHtml))
            b.Append($@"<p style=""font-size:13px;line-height:1.6;color:{TextMuted};margin:22px 0 0;text-align:center;"">⏳ {noteHtml}</p>");

        b.Append("</div>"); // hết content padding

        // Divider dots
        b.Append(@"<div style=""text-align:center;padding:22px 0 18px;"">");
        b.Append($@"<span style=""display:inline-block;width:10px;height:10px;background:{AccentMint};border-radius:50%;margin:0 4px;""></span>");
        b.Append($@"<span style=""display:inline-block;width:8px;height:8px;background:{AccentGold};border-radius:3px;margin:0 4px;""></span>");
        b.Append($@"<span style=""display:inline-block;width:12px;height:12px;background:{AccentMint};border-radius:50%;margin:0 4px;""></span>");
        b.Append($@"<span style=""display:inline-block;width:8px;height:8px;background:{AccentGold};border-radius:3px;margin:0 4px;""></span>");
        b.Append($@"<span style=""display:inline-block;width:10px;height:10px;background:{AccentMint};border-radius:50%;margin:0 4px;""></span>");
        b.Append("</div>");

        // Footer
        b.Append($@"<div style=""background:{BgInput};padding:16px 40px;font-size:11px;line-height:1.6;color:{TextMuted};text-align:center;border-top:1px solid {BorderMuted};"">Email tự động từ hệ thống <b style=""color:{TextPrimary};"">Nexus</b> — vui lòng không trả lời email này.<br>© {DateTime.UtcNow.Year} Nexus Commerce Platform. All rights reserved.</div>");

        b.Append("</div>"); // hết card
        b.Append("</div>"); // hết wrapper

        return b.ToString();
    }

    // ── Helper methods cho các loại email phổ biến ──────────────────────────

    public static string OrderConfirmation(
        string recipientName, Guid orderId, decimal total, decimal shippingFee,
        string address, DateTime? estimatedDelivery, string deliveryTypeLabel,
        string appUrl)
    {
        var deliveryInfo = estimatedDelivery.HasValue
            ? $"Dự kiến giao: <b>{estimatedDelivery.Value.ToLocalTime():dd/MM/yyyy HH:mm}</b>"
            : "Thời gian giao hàng sẽ được cập nhật sau.";

        var table = $@"
<div style=""background:#0a1628;border:1px solid #24344d;border-radius:10px;padding:18px;margin:20px 0;"">
  <table style=""width:100%;border-collapse:collapse;"">
    <tr><td style=""padding:7px 0;color:#94a3b8;font-size:13px;"">Mã đơn hàng</td>
        <td style=""padding:7px 0;color:#f1f5f9;font-weight:bold;text-align:right;font-family:monospace;font-size:13px;"">#{orderId.ToString().ToUpper()[..8]}</td></tr>
    <tr><td style=""padding:7px 0;color:#94a3b8;font-size:13px;"">Loại giao hàng</td>
        <td style=""padding:7px 0;color:#00d4aa;font-weight:bold;text-align:right;font-size:13px;"">{deliveryTypeLabel}</td></tr>
    <tr><td style=""padding:7px 0;color:#94a3b8;font-size:13px;"">Phí vận chuyển</td>
        <td style=""padding:7px 0;color:#f1f5f9;text-align:right;font-size:13px;"">{shippingFee:N0} VNĐ</td></tr>
    <tr style=""border-top:1px solid #24344d;"">
        <td style=""padding:10px 0 0;color:#f1f5f9;font-weight:bold;"">Tổng thanh toán</td>
        <td style=""padding:10px 0 0;color:#00d4aa;font-weight:bold;font-size:17px;text-align:right;"">{total:N0} VNĐ</td></tr>
  </table>
</div>";

        return Render(
            heading: "Đặt hàng thành công!",
            greetingName: recipientName,
            introHtml: $"Đơn hàng của bạn đã được ghi nhận. Chúng tôi sẽ xử lý và liên hệ xác nhận sớm nhất.",
            orderTableHtml: table,
            calloutLabel: "Thông tin giao hàng",
            calloutHtml: $"{deliveryInfo}<br>Địa chỉ: {address}",
            calloutKind: Callout.Info,
            ctaText: "Theo dõi đơn hàng",
            ctaUrl: $"{appUrl}/orders",
            noteHtml: "Đơn hàng sẽ được xác nhận trong vòng 1 giờ làm việc.");
    }

    public static string OrderCancelled(
        string recipientName, Guid orderId, string reason, bool wasOnlinePaid, string appUrl)
    {
        return Render(
            heading: "Đơn hàng đã bị hủy",
            greetingName: recipientName,
            introHtml: $"Đơn hàng <b>#{orderId.ToString().ToUpper()[..8]}</b> của bạn đã được hủy.",
            calloutLabel: "Lý do hủy",
            calloutHtml: reason,
            calloutKind: Callout.Danger,
            ctaText: "Mua sắm tiếp",
            ctaUrl: $"{appUrl}/products",
            noteHtml: wasOnlinePaid
                ? "Bạn đã thanh toán online. Tiền sẽ được hoàn trong 3–5 ngày làm việc."
                : null);
    }

    public static string ShipmentTracking(
        string recipientName, Guid orderId, string trackingNumber,
        string status, string? estimatedDelivery, string appUrl)
    {
        return Render(
            heading: "Cập nhật vận chuyển",
            greetingName: recipientName,
            introHtml: $"Đơn hàng <b>#{orderId.ToString().ToUpper()[..8]}</b> có cập nhật mới về tình trạng giao hàng.",
            calloutLabel: "Trạng thái",
            calloutHtml: $"{status}" + (estimatedDelivery != null ? $"<br>Dự kiến giao: {estimatedDelivery}" : ""),
            calloutKind: Callout.Info,
            ctaText: "Tra cứu vận đơn",
            ctaUrl: $"https://donhang.ghn.vn/?order_code={trackingNumber}",
            noteHtml: $"Mã vận đơn GHN: <b>{trackingNumber}</b>");
    }

    public static string AuctionWon(
        string recipientName, Guid auctionId, decimal winningPrice,
        string productTitle, string appUrl)
    {
        return Render(
            heading: "Bạn đã thắng đấu giá! 🎉",
            greetingName: recipientName,
            introHtml: $"Chúc mừng! Bạn đã thắng phiên đấu giá <b>{productTitle}</b>.",
            calloutLabel: "Giá thắng",
            calloutHtml: $"<b style=\"font-size:20px;color:#00d4aa;\">{winningPrice:N0} VNĐ</b>",
            calloutKind: Callout.Success,
            ctaText: "Tiến hành thanh toán",
            ctaUrl: $"{appUrl}/auctions/{auctionId}/checkout",
            noteHtml: "Vui lòng thanh toán trong vòng 24 giờ để giữ phiên đấu giá.");
    }

    public static string Welcome(string recipientName, string appUrl)
    {
        return Render(
            heading: "Chào mừng đến Nexus!",
            greetingName: recipientName,
            introHtml: "Tài khoản của bạn đã được tạo thành công. Bắt đầu khám phá hàng nghìn sản phẩm và tham gia đấu giá trực tiếp!",
            calloutLabel: "Dành riêng cho bạn",
            calloutHtml: "✔ Mua sắm không giới hạn<br>✔ Đấu giá trực tiếp realtime<br>✔ Giao hàng toàn quốc qua GHN<br>✔ Hỏa tốc trong bán kính 20km",
            calloutKind: Callout.Success,
            ctaText: "Bắt đầu mua sắm",
            ctaUrl: $"{appUrl}/products");
    }

    public static string VerifyEmail(string recipientName, string verifyUrl)
    {
        return Render(
            heading: "Xác thực tài khoản Nexus",
            greetingName: recipientName,
            introHtml: "Cảm ơn bạn đã đăng ký tài khoản. Vui lòng xác thực email để bắt đầu mua sắm và tham gia đấu giá.",
            calloutLabel: "Bước tiếp theo",
            calloutHtml: "Click nút bên dưới để xác thực email của bạn.<br><span style=\"font-size:12px;color:#94a3b8;\">Link này có hiệu lực trong 24 giờ.</span>",
            calloutKind: Callout.Info,
            ctaText: "Xác thực email ngay",
            ctaUrl: verifyUrl,
            noteHtml: "Nếu bạn không đăng ký tài khoản Nexus, hãy bỏ qua email này.");
    }
}

namespace Nexus.FulfillmentService.Infrastructure.Adapters.Ghn;

public class GhnSettings
{
    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api";
    public string Token { get; set; } = default!;
    public int ShopId { get; set; }
    // Quận của shop/kho hàng (người bán) - dùng để tính phí và tạo đơn
    public int FromDistrictId { get; set; }
    public string FromWardCode { get; set; } = default!;
    // Service type: 1=Express, 2=Standard, 3=Saving
    public int DefaultServiceTypeId { get; set; } = 2;
}

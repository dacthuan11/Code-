using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

public class SellerProfile : BaseEntity
{
    public Guid   UserId      { get; private set; }
    public string ShopName    { get; private set; } = default!;
    public string ShopSlug    { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string LogoUrl     { get; private set; } = string.Empty;
    public string BannerUrl   { get; private set; } = string.Empty;
    public bool   IsVerified  { get; private set; }
    public double Rating      { get; private set; }
    public int    TotalSales  { get; private set; }

    private SellerProfile() { }

    public static SellerProfile Create(Guid userId, string shopName, string description)
    {
        var slug = shopName.ToLower()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("á","a").Replace("à","a").Replace("ả","a").Replace("ã","a").Replace("ạ","a")
            .Replace("ă","a").Replace("â","a")
            + "-" + userId.ToString()[..8];
        return new SellerProfile
        {
            UserId = userId, ShopName = shopName,
            ShopSlug = slug, Description = description
        };
    }

    public void UpdateProfile(string shopName, string description, string logoUrl, string bannerUrl)
    {
        ShopName = shopName; Description = description;
        if (!string.IsNullOrWhiteSpace(logoUrl))   LogoUrl   = logoUrl;
        if (!string.IsNullOrWhiteSpace(bannerUrl)) BannerUrl = bannerUrl;
        Touch();
    }

    public void UpdateRating(double rating, int totalSales)
    {
        Rating = Math.Round(rating, 1); TotalSales = totalSales; Touch();
    }

    public void Verify() { IsVerified = true; Touch(); }
}

using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

public class UserAddress : BaseEntity
{
    public Guid   UserId        { get; private set; }
    public string Label         { get; private set; } = default!;  // "Nhà", "Văn phòng"
    public string RecipientName { get; private set; } = default!;
    public string Phone         { get; private set; } = default!;
    public string AddressLine   { get; private set; } = default!;
    public string Province      { get; private set; } = default!;
    public string District      { get; private set; } = default!;
    public string Ward          { get; private set; } = default!;
    public int    ToDistrictId  { get; private set; }
    public string ToWardCode    { get; private set; } = default!;
    public bool   IsDefault     { get; private set; }

    private UserAddress() { }

    public static UserAddress Create(Guid userId, string label, string recipientName,
        string phone, string addressLine, string province, string district, string ward,
        int toDistrictId, string toWardCode, bool isDefault = false) => new()
    {
        UserId = userId, Label = label, RecipientName = recipientName,
        Phone = phone, AddressLine = addressLine, Province = province,
        District = district, Ward = ward,
        ToDistrictId = toDistrictId, ToWardCode = toWardCode, IsDefault = isDefault
    };

    public void Update(string label, string recipientName, string phone,
        string addressLine, string province, string district, string ward,
        int toDistrictId, string toWardCode)
    {
        Label = label; RecipientName = recipientName; Phone = phone;
        AddressLine = addressLine; Province = province;
        District = district; Ward = ward;
        ToDistrictId = toDistrictId; ToWardCode = toWardCode;
        Touch();
    }

    public void SetDefault()   { IsDefault = true;  Touch(); }
    public void UnsetDefault() { IsDefault = false; Touch(); }
}

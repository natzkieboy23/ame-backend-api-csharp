namespace InventoryApi.DTOs.InventorySite;

public class InventorySiteUpdateDto
{
    public string? Name { get; set; }
    public string? IsActive { get; set; }
    public string? ParentSiteRef_ListID { get; set; }
    public string? ParentSiteRef_FullName { get; set; }
    public string? IsDefaultSite { get; set; }
    public string? SiteDesc { get; set; }
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? SiteAddress_Addr1 { get; set; }
    public string? SiteAddress_Addr2 { get; set; }
    public string? SiteAddress_Addr3 { get; set; }
    public string? SiteAddress_Addr4 { get; set; }
    public string? SiteAddress_Addr5 { get; set; }
    public string? SiteAddress_City { get; set; }
    public string? SiteAddress_State { get; set; }
    public string? SiteAddress_PostalCode { get; set; }
    public string? SiteAddress_Country { get; set; }
    public string? SiteAddressBlock_Addr1 { get; set; }
    public string? SiteAddressBlock_Addr2 { get; set; }
    public string? SiteAddressBlock_Addr3 { get; set; }
    public string? SiteAddressBlock_Addr4 { get; set; }
    public string? SiteAddressBlock_Addr5 { get; set; }
    public string? UserData { get; set; }
    public string? Status { get; set; }
}

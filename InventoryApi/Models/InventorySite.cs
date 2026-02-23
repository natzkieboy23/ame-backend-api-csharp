using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("inventorysite")]
public class InventorySite
{
    [Key]
    [Column("ListID")]
    [MaxLength(36)]
    public string ListID { get; set; } = string.Empty;

    [Column("TimeCreated")]
    public DateTime? TimeCreated { get; set; }

    [Column("TimeModified")]
    public DateTime? TimeModified { get; set; }

    [Column("EditSequence")]
    [MaxLength(16)]
    public string? EditSequence { get; set; }

    [Column("Name")]
    [MaxLength(31)]
    public string? Name { get; set; }

    [Column("IsActive")]
    [MaxLength(5)]
    public string? IsActive { get; set; }

    [Column("ParentSiteRef_ListID")]
    [MaxLength(36)]
    public string? ParentSiteRef_ListID { get; set; }

    [Column("ParentSiteRef_FullName")]
    [MaxLength(209)]
    public string? ParentSiteRef_FullName { get; set; }

    [Column("IsDefaultSite")]
    [MaxLength(5)]
    public string? IsDefaultSite { get; set; }

    [Column("SiteDesc")]
    [MaxLength(255)]
    public string? SiteDesc { get; set; }

    [Column("Contact")]
    [MaxLength(100)]
    public string? Contact { get; set; }

    [Column("Phone")]
    [MaxLength(21)]
    public string? Phone { get; set; }

    [Column("Fax")]
    [MaxLength(21)]
    public string? Fax { get; set; }

    [Column("Email")]
    [MaxLength(1000)]
    public string? Email { get; set; }

    [Column("SiteAddress_Addr1")] [MaxLength(41)] public string? SiteAddress_Addr1 { get; set; }
    [Column("SiteAddress_Addr2")] [MaxLength(41)] public string? SiteAddress_Addr2 { get; set; }
    [Column("SiteAddress_Addr3")] [MaxLength(41)] public string? SiteAddress_Addr3 { get; set; }
    [Column("SiteAddress_Addr4")] [MaxLength(41)] public string? SiteAddress_Addr4 { get; set; }
    [Column("SiteAddress_Addr5")] [MaxLength(41)] public string? SiteAddress_Addr5 { get; set; }
    [Column("SiteAddress_City")] [MaxLength(31)] public string? SiteAddress_City { get; set; }
    [Column("SiteAddress_State")] [MaxLength(21)] public string? SiteAddress_State { get; set; }
    [Column("SiteAddress_PostalCode")] [MaxLength(13)] public string? SiteAddress_PostalCode { get; set; }
    [Column("SiteAddress_Country")] [MaxLength(31)] public string? SiteAddress_Country { get; set; }

    [Column("SiteAddressBlock_Addr1")] [MaxLength(41)] public string? SiteAddressBlock_Addr1 { get; set; }
    [Column("SiteAddressBlock_Addr2")] [MaxLength(41)] public string? SiteAddressBlock_Addr2 { get; set; }
    [Column("SiteAddressBlock_Addr3")] [MaxLength(41)] public string? SiteAddressBlock_Addr3 { get; set; }
    [Column("SiteAddressBlock_Addr4")] [MaxLength(41)] public string? SiteAddressBlock_Addr4 { get; set; }
    [Column("SiteAddressBlock_Addr5")] [MaxLength(41)] public string? SiteAddressBlock_Addr5 { get; set; }

    [Column("UserData")]
    [MaxLength(255)]
    public string? UserData { get; set; }

    [Column("Status")]
    [MaxLength(10)]
    public string? Status { get; set; }
}

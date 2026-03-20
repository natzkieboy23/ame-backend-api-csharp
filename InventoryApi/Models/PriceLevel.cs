using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("pricelevel")]
public class PriceLevel
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

    [Column("PriceLevelType")]
    [MaxLength(36)]
    public string? PriceLevelType { get; set; }

    [Column("PriceLevelFixedPercentage")]
    [MaxLength(159)]
    public string? PriceLevelFixedPercentage { get; set; }

    [Column("CurrencyRef_ListID")]
    [MaxLength(36)]
    public string? CurrencyRef_ListID { get; set; }

    [Column("CurrencyRef_FullName")]
    [MaxLength(64)]
    public string? CurrencyRef_FullName { get; set; }

    [Column("UserData")]
    [MaxLength(255)]
    public string? UserData { get; set; }

    [Column("Status")]
    [MaxLength(10)]
    public string? Status { get; set; }

    public ICollection<PriceLevelPerItemDetail> PerItemDetails { get; set; } = [];
}

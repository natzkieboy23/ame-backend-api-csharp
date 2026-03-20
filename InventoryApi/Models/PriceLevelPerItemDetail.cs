using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("pricelevelperitemdetail")]
public class PriceLevelPerItemDetail
{
    [Column("IDKEY")]
    [MaxLength(255)]
    public string IDKEY { get; set; } = string.Empty;

    [Column("SeqNum")]
    public int SeqNum { get; set; }

    [Column("ItemRef_ListID")]
    [MaxLength(36)]
    public string? ItemRef_ListID { get; set; }

    [Column("ItemRef_FullName")]
    [MaxLength(36)]
    public string? ItemRef_FullName { get; set; }

    [Column("CustomPrice")]
    public decimal? CustomPrice { get; set; }

    [Column("CustomPricePercent")]
    [MaxLength(255)]
    public string? CustomPricePercent { get; set; }

    [Column("AdjustPercentage")]
    [MaxLength(255)]
    public string? AdjustPercentage { get; set; }

    [Column("AdjustRelativeTo")]
    [MaxLength(255)]
    public string? AdjustRelativeTo { get; set; }

    [Column("GroupIDKEY")]
    [MaxLength(255)]
    public string? GroupIDKEY { get; set; }
}

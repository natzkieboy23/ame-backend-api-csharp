using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("itemsites")]
public class ItemSite
{
    [Key]
    [Column("ListID")]
    [MaxLength(36)]
    public string ListID { get; set; } = string.Empty;

    [Column("TimeCreated")]  public DateTime? TimeCreated  { get; set; }
    [Column("TimeModified")] public DateTime? TimeModified { get; set; }
    [Column("EditSequence")] [MaxLength(16)]  public string? EditSequence { get; set; }

    [Column("ItemInventoryAssemblyRef_ListID")]   [MaxLength(36)]  public string? ItemInventoryAssemblyRef_ListID   { get; set; }
    [Column("ItemInventoryAssemblyRef_FullName")] [MaxLength(159)] public string? ItemInventoryAssemblyRef_FullName { get; set; }

    [Column("ItemInventoryRef_ListID")]   [MaxLength(36)]  public string? ItemInventoryRef_ListID   { get; set; }
    [Column("ItemInventoryRef_FullName")] [MaxLength(159)] public string? ItemInventoryRef_FullName { get; set; }

    [Column("InventorySiteRef_ListID")]   [MaxLength(36)]  public string? InventorySiteRef_ListID   { get; set; }
    [Column("InventorySiteRef_FullName")] [MaxLength(231)] public string? InventorySiteRef_FullName { get; set; }

    [Column("InventorySiteLocationRef_ListID")]   [MaxLength(36)]  public string? InventorySiteLocationRef_ListID   { get; set; }
    [Column("InventorySiteLocationRef_FullName")] [MaxLength(209)] public string? InventorySiteLocationRef_FullName { get; set; }

    [Column("ReorderLevel")] [MaxLength(255)] public string?  ReorderLevel  { get; set; }

    [Column("QuantityOnHand")]                        public decimal? QuantityOnHand                        { get; set; }
    [Column("QuantityOnPurchaseOrders")]              public decimal? QuantityOnPurchaseOrders              { get; set; }
    [Column("QuantityOnSalesOrders")]                 public decimal? QuantityOnSalesOrders                 { get; set; }
    [Column("QuantityToBeBuiltByPendingBuildTxns")]   public decimal? QuantityToBeBuiltByPendingBuildTxns   { get; set; }
    [Column("QuantityRequiredByPendingBuildTxns")]    public decimal? QuantityRequiredByPendingBuildTxns    { get; set; }
    [Column("QuantityOnPendingTransfers")]            public decimal? QuantityOnPendingTransfers            { get; set; }
    [Column("AssemblyBuildPoint")]                    public decimal? AssemblyBuildPoint                    { get; set; }

    [Column("UserData")] [MaxLength(255)] public string? UserData { get; set; }
    [Column("Status")]   [MaxLength(10)]  public string? Status   { get; set; }
}

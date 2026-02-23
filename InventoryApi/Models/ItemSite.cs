using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("itemsites")]
public class ItemSite
{
    [Key]
    [Column("ListID")]
    public string ListID { get; set; } = string.Empty;

    [Column("TimeCreated")]
    public DateTime? TimeCreated { get; set; }

    [Column("TimeModified")]
    public DateTime? TimeModified { get; set; }

    [Column("ItemInventoryRef_ListID")]
    public string? ItemInventoryRef_ListID { get; set; }

    [Column("ItemInventoryRef_FullName")]
    public string? ItemInventoryRef_FullName { get; set; }

    [Column("InventorySiteRef_ListID")]
    public string? InventorySiteRef_ListID { get; set; }

    [Column("InventorySiteRef_FullName")]
    public string? InventorySiteRef_FullName { get; set; }

    [Column("ReorderLevel")]
    public string? ReorderLevel { get; set; }

    [Column("QuantityOnHand")]
    public decimal? QuantityOnHand { get; set; }

    [Column("QuantityOnPurchaseOrders")]
    public decimal? QuantityOnPurchaseOrders { get; set; }

    [Column("QuantityOnSalesOrders")]
    public decimal? QuantityOnSalesOrders { get; set; }

    [Column("QuantityOnPendingTransfers")]
    public decimal? QuantityOnPendingTransfers { get; set; }

    [Column("Status")]
    public string? Status { get; set; }
}

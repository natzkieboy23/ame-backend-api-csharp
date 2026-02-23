using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("iteminventory")]
public class ItemInventory
{
    [Key]
    [Column("ListID")]
    [MaxLength(36)]
    public string ListID { get; set; } = string.Empty;

    [Column("TimeCreated")] public DateTime? TimeCreated { get; set; }
    [Column("TimeModified")] public DateTime? TimeModified { get; set; }
    [Column("EditSequence")] [MaxLength(16)] public string? EditSequence { get; set; }

    [Column("Name")] [MaxLength(31)] public string? Name { get; set; }
    [Column("FullName")] [MaxLength(159)] public string? FullName { get; set; }
    [Column("BarCodeValue")] [MaxLength(50)] public string? BarCodeValue { get; set; }
    [Column("IsActive")] [MaxLength(5)] public string? IsActive { get; set; }

    [Column("ClassRef_ListID")] [MaxLength(36)] public string? ClassRef_ListID { get; set; }
    [Column("ClassRef_FullName")] [MaxLength(159)] public string? ClassRef_FullName { get; set; }
    [Column("ParentRef_ListID")] [MaxLength(36)] public string? ParentRef_ListID { get; set; }
    [Column("ParentRef_FullName")] [MaxLength(209)] public string? ParentRef_FullName { get; set; }
    [Column("Sublevel")] public int? Sublevel { get; set; }

    [Column("ManufacturerPartNumber")] [MaxLength(31)] public string? ManufacturerPartNumber { get; set; }

    [Column("UnitOfMeasureSetRef_ListID")] [MaxLength(36)] public string? UnitOfMeasureSetRef_ListID { get; set; }
    [Column("UnitOfMeasureSetRef_FullName")] [MaxLength(31)] public string? UnitOfMeasureSetRef_FullName { get; set; }

    [Column("IsTaxIncluded")] [MaxLength(5)] public string? IsTaxIncluded { get; set; }
    [Column("SalesTaxCodeRef_ListID")] [MaxLength(36)] public string? SalesTaxCodeRef_ListID { get; set; }
    [Column("SalesTaxCodeRef_FullName")] [MaxLength(3)] public string? SalesTaxCodeRef_FullName { get; set; }

    [Column("SalesDesc")] [MaxLength(1000)] public string? SalesDesc { get; set; }
    [Column("SalesPrice")] public decimal? SalesPrice { get; set; }

    [Column("IncomeAccountRef_ListID")] [MaxLength(36)] public string? IncomeAccountRef_ListID { get; set; }
    [Column("IncomeAccountRef_FullName")] [MaxLength(159)] public string? IncomeAccountRef_FullName { get; set; }

    [Column("PurchaseDesc")] [MaxLength(1000)] public string? PurchaseDesc { get; set; }
    [Column("PurchaseCost")] public decimal? PurchaseCost { get; set; }

    [Column("COGSAccountRef_ListID")] [MaxLength(36)] public string? COGSAccountRef_ListID { get; set; }
    [Column("COGSAccountRef_FullName")] [MaxLength(159)] public string? COGSAccountRef_FullName { get; set; }

    [Column("PrefVendorRef_ListID")] [MaxLength(36)] public string? PrefVendorRef_ListID { get; set; }
    [Column("PrefVendorRef_FullName")] [MaxLength(41)] public string? PrefVendorRef_FullName { get; set; }

    [Column("AssetAccountRef_ListID")] [MaxLength(36)] public string? AssetAccountRef_ListID { get; set; }
    [Column("AssetAccountRef_FullName")] [MaxLength(159)] public string? AssetAccountRef_FullName { get; set; }

    [Column("ReorderPoint")] public int? ReorderPoint { get; set; }
    [Column("Max")] public int? Max { get; set; }

    [Column("QuantityOnHand")] public decimal? QuantityOnHand { get; set; }
    [Column("AverageCost")] public decimal? AverageCost { get; set; }
    [Column("QuantityOnOrder")] public decimal? QuantityOnOrder { get; set; }
    [Column("QuantityOnSalesOrder")] public decimal? QuantityOnSalesOrder { get; set; }

    [Column("UserData")] [MaxLength(255)] public string? UserData { get; set; }

    [Column("CustomField1")]  [MaxLength(50)] public string? CustomField1  { get; set; }
    [Column("CustomField2")]  [MaxLength(50)] public string? CustomField2  { get; set; }
    [Column("CustomField3")]  [MaxLength(50)] public string? CustomField3  { get; set; }
    [Column("CustomField4")]  [MaxLength(50)] public string? CustomField4  { get; set; }
    [Column("CustomField5")]  [MaxLength(50)] public string? CustomField5  { get; set; }
    [Column("CustomField6")]  [MaxLength(50)] public string? CustomField6  { get; set; }
    [Column("CustomField7")]  [MaxLength(50)] public string? CustomField7  { get; set; }
    [Column("CustomField8")]  [MaxLength(50)] public string? CustomField8  { get; set; }
    [Column("CustomField9")]  [MaxLength(50)] public string? CustomField9  { get; set; }
    [Column("CustomField10")] [MaxLength(50)] public string? CustomField10 { get; set; }
    [Column("CustomField11")] [MaxLength(50)] public string? CustomField11 { get; set; }
    [Column("CustomField12")] [MaxLength(50)] public string? CustomField12 { get; set; }
    [Column("CustomField13")] [MaxLength(50)] public string? CustomField13 { get; set; }
    [Column("CustomField14")] [MaxLength(50)] public string? CustomField14 { get; set; }
    [Column("CustomField15")] [MaxLength(50)] public string? CustomField15 { get; set; }

    [Column("Status")] [MaxLength(10)] public string? Status { get; set; }
    [Column("ExternalGUID")] [MaxLength(40)] public string? ExternalGUID { get; set; }
}

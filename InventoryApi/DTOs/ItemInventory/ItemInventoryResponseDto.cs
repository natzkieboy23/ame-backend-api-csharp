namespace InventoryApi.DTOs.ItemInventory;

public class ItemInventoryResponseDto
{
    public string ListID { get; set; } = string.Empty;
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }
    public string? EditSequence { get; set; }
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string? BarCodeValue { get; set; }
    public string? IsActive { get; set; }
    public string? ClassRef_ListID { get; set; }
    public string? ClassRef_FullName { get; set; }
    public string? ParentRef_ListID { get; set; }
    public string? ParentRef_FullName { get; set; }
    public int? Sublevel { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? UnitOfMeasureSetRef_ListID { get; set; }
    public string? UnitOfMeasureSetRef_FullName { get; set; }
    public string? IsTaxIncluded { get; set; }
    public string? SalesTaxCodeRef_ListID { get; set; }
    public string? SalesTaxCodeRef_FullName { get; set; }
    public string? SalesDesc { get; set; }
    public decimal? SalesPrice { get; set; }
    public string? IncomeAccountRef_ListID { get; set; }
    public string? IncomeAccountRef_FullName { get; set; }
    public string? PurchaseDesc { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string? COGSAccountRef_ListID { get; set; }
    public string? COGSAccountRef_FullName { get; set; }
    public string? PrefVendorRef_ListID { get; set; }
    public string? PrefVendorRef_FullName { get; set; }
    public string? AssetAccountRef_ListID { get; set; }
    public string? AssetAccountRef_FullName { get; set; }
    public int? ReorderPoint { get; set; }
    public int? Max { get; set; }
    public decimal? QuantityOnHand { get; set; }
    public decimal? AverageCost { get; set; }
    public decimal? QuantityOnOrder { get; set; }
    public decimal? QuantityOnSalesOrder { get; set; }
    public string? UserData { get; set; }
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
    public string? CustomField3 { get; set; }
    public string? CustomField4 { get; set; }
    public string? CustomField5 { get; set; }
    public string? CustomField6 { get; set; }
    public string? CustomField7 { get; set; }
    public string? CustomField8 { get; set; }
    public string? CustomField9 { get; set; }
    public string? CustomField10 { get; set; }
    public string? CustomField11 { get; set; }
    public string? CustomField12 { get; set; }
    public string? CustomField13 { get; set; }
    public string? CustomField14 { get; set; }
    public string? CustomField15 { get; set; }
    public string? Status { get; set; }
    public string? ExternalGUID { get; set; }
    // Populated only when ?siteFullName= is provided — from itemsites join
    public decimal? SiteQuantityOnHand { get; set; }
    // Warehouse price level (pricelevel.Name LIKE 'W%') custom price; 0.00 if not set
    public decimal  WarehousePrice     { get; set; } = 0m;
}

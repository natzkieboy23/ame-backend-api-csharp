namespace InventoryApi.DTOs.TxnItemLine;

public class TxnItemLineResponseDto
{
    public string  TxnLineID                  { get; set; } = string.Empty;
    public string  IDKEY                      { get; set; } = string.Empty;
    public int?    SeqNum                     { get; set; }
    public string? ItemRef_ListID             { get; set; }
    public string? ItemRef_FullName           { get; set; }
    public string? Description               { get; set; }
    public decimal? Quantity                 { get; set; }
    public string? UnitOfMeasure             { get; set; }
    public string?  Cost                     { get; set; }
    public decimal? Amount                   { get; set; }
    public string? InventorySiteRef_ListID   { get; set; }
    public string? InventorySiteRef_FullName { get; set; }
    public string? SerialNumber              { get; set; }
    public string? LotNumber                 { get; set; }
    public string? SalesTaxCodeRef_ListID    { get; set; }
    public string? SalesTaxCodeRef_FullName  { get; set; }
    public string? ClassRef_ListID           { get; set; }
    public string? ClassRef_FullName         { get; set; }
    public string? CustomField1              { get; set; }
    public string? CustomField2              { get; set; }
    public string? CustomField3              { get; set; }
    public string? CustomField4              { get; set; }
    public string? CustomField5              { get; set; }
    public string? CustomField6              { get; set; }
    public string? CustomField7              { get; set; }
    public string? CustomField8              { get; set; }
    public string? CustomField9              { get; set; }
    public string? CustomField10             { get; set; }
    public string? CustomField11             { get; set; }
    public string? CustomField12             { get; set; }
    public string? CustomField13             { get; set; }
    public string? CustomField14             { get; set; }
    public string? CustomField15             { get; set; }
}

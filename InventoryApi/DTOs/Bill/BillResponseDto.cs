using InventoryApi.DTOs.TxnItemLine;

namespace InventoryApi.DTOs.Bill;

public class BillResponseDto
{
    public string TxnID                     { get; set; } = string.Empty;
    public DateTime? TimeCreated            { get; set; }
    public DateTime? TimeModified           { get; set; }
    public string? EditSequence             { get; set; }
    public int? TxnNumber                   { get; set; }
    public string? VendorRef_ListID         { get; set; }
    public string? VendorRef_FullName       { get; set; }
    public string? APAccountRef_ListID      { get; set; }
    public string? APAccountRef_FullName    { get; set; }
    public DateTime? TxnDate               { get; set; }
    public DateTime? DueDate               { get; set; }
    public decimal? AmountDue              { get; set; }
    public string? CurrencyRef_ListID       { get; set; }
    public string? CurrencyRef_FullName     { get; set; }
    public decimal? ExchangeRate            { get; set; }
    public decimal? AmountDueInHomeCurrency { get; set; }
    public string? RefNumber               { get; set; }
    public string? TermsRef_ListID          { get; set; }
    public string? TermsRef_FullName        { get; set; }
    public string? Memo                    { get; set; }
    public string? IsTaxIncluded           { get; set; }
    public string? SalesTaxCodeRef_ListID   { get; set; }
    public string? SalesTaxCodeRef_FullName { get; set; }
    public string? IsPaid                  { get; set; }
    public string? VendorAddress_Addr1      { get; set; }
    public string? VendorAddress_Addr2      { get; set; }
    public string? VendorAddress_Addr3      { get; set; }
    public string? VendorAddress_Addr4      { get; set; }
    public string? VendorAddress_Addr5      { get; set; }
    public string? VendorAddress_City       { get; set; }
    public string? VendorAddress_State      { get; set; }
    public string? VendorAddress_PostalCode { get; set; }
    public string? VendorAddress_Country    { get; set; }
    public string? VendorAddress_Note       { get; set; }
    public decimal? OpenAmount             { get; set; }
    public string? UserData                { get; set; }
    public string? CustomField1            { get; set; }
    public string? CustomField2            { get; set; }
    public string? CustomField3            { get; set; }
    public string? CustomField4            { get; set; }
    public string? CustomField5            { get; set; }
    public string? CustomField6            { get; set; }
    public string? CustomField7            { get; set; }
    public string? CustomField8            { get; set; }
    public string? CustomField9            { get; set; }
    public string? CustomField10           { get; set; }
    public string? CustomField11           { get; set; }
    public string? CustomField12           { get; set; }
    public string? CustomField13           { get; set; }
    public string? CustomField14           { get; set; }
    public string? CustomField15           { get; set; }
    public string? Status                  { get; set; }
    public string? ExternalGUID            { get; set; }

    public List<TxnItemLineResponseDto> LineItems { get; set; } = new();
}

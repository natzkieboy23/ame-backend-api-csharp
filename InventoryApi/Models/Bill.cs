using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("bill")]
public class Bill
{
    [Key]
    [Column("TxnID")]
    [MaxLength(36)]
    public string TxnID { get; set; } = string.Empty;

    [Column("TimeCreated")]  public DateTime? TimeCreated  { get; set; }
    [Column("TimeModified")] public DateTime? TimeModified { get; set; }
    [Column("EditSequence")] [MaxLength(16)] public string? EditSequence { get; set; }
    [Column("TxnNumber")]    public int? TxnNumber { get; set; }

    [Column("VendorRef_ListID")]   [MaxLength(36)] public string? VendorRef_ListID   { get; set; }
    [Column("VendorRef_FullName")] [MaxLength(41)] public string? VendorRef_FullName { get; set; }

    [Column("APAccountRef_ListID")]   [MaxLength(36)]  public string? APAccountRef_ListID   { get; set; }
    [Column("APAccountRef_FullName")] [MaxLength(159)] public string? APAccountRef_FullName { get; set; }

    [Column("TxnDate")] public DateTime? TxnDate { get; set; }
    [Column("DueDate")] public DateTime? DueDate  { get; set; }
    [Column("AmountDue")] public decimal? AmountDue { get; set; }

    [Column("CurrencyRef_ListID")]   [MaxLength(36)] public string? CurrencyRef_ListID   { get; set; }
    [Column("CurrencyRef_FullName")] [MaxLength(64)] public string? CurrencyRef_FullName { get; set; }
    [Column("ExchangeRate")]             public decimal? ExchangeRate             { get; set; }
    [Column("AmountDueInHomeCurrency")]  public decimal? AmountDueInHomeCurrency  { get; set; }

    [Column("RefNumber")]  [MaxLength(25)] public string? RefNumber  { get; set; }

    [Column("TermsRef_ListID")]   [MaxLength(36)] public string? TermsRef_ListID   { get; set; }
    [Column("TermsRef_FullName")] [MaxLength(31)] public string? TermsRef_FullName { get; set; }

    [Column("Memo")]           [MaxLength(1000)] public string? Memo           { get; set; }
    [Column("IsTaxIncluded")]  [MaxLength(5)]    public string? IsTaxIncluded  { get; set; }

    [Column("SalesTaxCodeRef_ListID")]   [MaxLength(36)] public string? SalesTaxCodeRef_ListID   { get; set; }
    [Column("SalesTaxCodeRef_FullName")] [MaxLength(5)]  public string? SalesTaxCodeRef_FullName { get; set; }

    [Column("IsPaid")] [MaxLength(5)] public string? IsPaid { get; set; }

    [Column("VendorAddress_Addr1")]      [MaxLength(41)] public string? VendorAddress_Addr1      { get; set; }
    [Column("VendorAddress_Addr2")]      [MaxLength(41)] public string? VendorAddress_Addr2      { get; set; }
    [Column("VendorAddress_Addr3")]      [MaxLength(41)] public string? VendorAddress_Addr3      { get; set; }
    [Column("VendorAddress_Addr4")]      [MaxLength(41)] public string? VendorAddress_Addr4      { get; set; }
    [Column("VendorAddress_Addr5")]      [MaxLength(41)] public string? VendorAddress_Addr5      { get; set; }
    [Column("VendorAddress_City")]       [MaxLength(31)] public string? VendorAddress_City       { get; set; }
    [Column("VendorAddress_State")]      [MaxLength(21)] public string? VendorAddress_State      { get; set; }
    [Column("VendorAddress_PostalCode")] [MaxLength(13)] public string? VendorAddress_PostalCode { get; set; }
    [Column("VendorAddress_Country")]    [MaxLength(21)] public string? VendorAddress_Country    { get; set; }
    [Column("VendorAddress_Note")]       [MaxLength(41)] public string? VendorAddress_Note       { get; set; }

    [Column("OpenAmount")] public decimal? OpenAmount { get; set; }
    [Column("UserData")]   [MaxLength(255)] public string? UserData   { get; set; }

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

    [Column("Status")]       [MaxLength(10)] public string? Status       { get; set; }
    [Column("ExternalGUID")] [MaxLength(40)] public string? ExternalGUID { get; set; }

    // Navigation
    public ICollection<TxnItemLineDetail> LineItems { get; set; } = new List<TxnItemLineDetail>();
}

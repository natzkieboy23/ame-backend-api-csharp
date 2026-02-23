using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("txnitemlinedetail")]
public class TxnItemLineDetail
{
    [Key]
    [Column("TxnLineID")]
    [MaxLength(36)]
    public string TxnLineID { get; set; } = string.Empty;

    [Column("IDKEY")]
    [MaxLength(255)]
    public string IDKEY { get; set; } = string.Empty;

    [Column("SeqNum")]
    public int? SeqNum { get; set; }

    [Column("ItemRef_ListID")]   [MaxLength(36)]  public string? ItemRef_ListID   { get; set; }
    [Column("ItemRef_FullName")] [MaxLength(209)] public string? ItemRef_FullName { get; set; }

    [Column("Description")]   [MaxLength(1000)] public string? Description   { get; set; }
    [Column("Quantity")]      public decimal? Quantity      { get; set; }
    [Column("UnitOfMeasure")] [MaxLength(25)]  public string? UnitOfMeasure  { get; set; }
    [Column("Cost")]          [MaxLength(255)] public string?  Cost          { get; set; }
    [Column("Amount")]        public decimal? Amount        { get; set; }

    [Column("InventorySiteRef_ListID")]   [MaxLength(36)]  public string? InventorySiteRef_ListID   { get; set; }
    [Column("InventorySiteRef_FullName")] [MaxLength(209)] public string? InventorySiteRef_FullName { get; set; }

    [Column("SerialNumber")] [MaxLength(1000)] public string? SerialNumber { get; set; }
    [Column("LotNumber")]    [MaxLength(40)]   public string? LotNumber    { get; set; }

    [Column("SalesTaxCodeRef_ListID")]   [MaxLength(36)] public string? SalesTaxCodeRef_ListID   { get; set; }
    [Column("SalesTaxCodeRef_FullName")] [MaxLength(5)]  public string? SalesTaxCodeRef_FullName { get; set; }

    [Column("ClassRef_ListID")]   [MaxLength(36)]  public string? ClassRef_ListID   { get; set; }
    [Column("ClassRef_FullName")] [MaxLength(209)] public string? ClassRef_FullName { get; set; }

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
}

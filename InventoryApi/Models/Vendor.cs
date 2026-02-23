using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

[Table("vendor")]
public class Vendor
{
    [Key]
    [Column("ListID")]
    [MaxLength(36)]
    public string ListID { get; set; } = string.Empty;

    [Column("TimeCreated")] public DateTime? TimeCreated { get; set; }
    [Column("TimeModified")] public DateTime? TimeModified { get; set; }
    [Column("EditSequence")] [MaxLength(36)] public string? EditSequence { get; set; }

    [Column("Name")] [MaxLength(255)] public string? Name { get; set; }
    [Column("IsActive")] [MaxLength(5)] public string? IsActive { get; set; }
    [Column("CompanyName")] [MaxLength(41)] public string? CompanyName { get; set; }
    [Column("Salutation")] [MaxLength(15)] public string? Salutation { get; set; }
    [Column("FirstName")] [MaxLength(25)] public string? FirstName { get; set; }
    [Column("MiddleName")] [MaxLength(5)] public string? MiddleName { get; set; }
    [Column("LastName")] [MaxLength(25)] public string? LastName { get; set; }
    [Column("Suffix")] [MaxLength(41)] public string? Suffix { get; set; }
    [Column("JobTitle")] [MaxLength(41)] public string? JobTitle { get; set; }

    [Column("VendorAddress_Addr1")] [MaxLength(41)] public string? VendorAddress_Addr1 { get; set; }
    [Column("VendorAddress_Addr2")] [MaxLength(41)] public string? VendorAddress_Addr2 { get; set; }
    [Column("VendorAddress_Addr3")] [MaxLength(41)] public string? VendorAddress_Addr3 { get; set; }
    [Column("VendorAddress_Addr4")] [MaxLength(41)] public string? VendorAddress_Addr4 { get; set; }
    [Column("VendorAddress_Addr5")] [MaxLength(41)] public string? VendorAddress_Addr5 { get; set; }
    [Column("VendorAddress_City")] [MaxLength(31)] public string? VendorAddress_City { get; set; }
    [Column("VendorAddress_State")] [MaxLength(21)] public string? VendorAddress_State { get; set; }
    [Column("VendorAddress_PostalCode")] [MaxLength(13)] public string? VendorAddress_PostalCode { get; set; }
    [Column("VendorAddress_Country")] [MaxLength(31)] public string? VendorAddress_Country { get; set; }
    [Column("VendorAddress_Note")] [MaxLength(41)] public string? VendorAddress_Note { get; set; }

    [Column("ShipAddress_Addr1")] [MaxLength(41)] public string? ShipAddress_Addr1 { get; set; }
    [Column("ShipAddress_Addr2")] [MaxLength(41)] public string? ShipAddress_Addr2 { get; set; }
    [Column("ShipAddress_Addr3")] [MaxLength(41)] public string? ShipAddress_Addr3 { get; set; }
    [Column("ShipAddress_Addr4")] [MaxLength(41)] public string? ShipAddress_Addr4 { get; set; }
    [Column("ShipAddress_Addr5")] [MaxLength(41)] public string? ShipAddress_Addr5 { get; set; }
    [Column("ShipAddress_City")] [MaxLength(31)] public string? ShipAddress_City { get; set; }
    [Column("ShipAddress_State")] [MaxLength(21)] public string? ShipAddress_State { get; set; }
    [Column("ShipAddress_PostalCode")] [MaxLength(13)] public string? ShipAddress_PostalCode { get; set; }
    [Column("ShipAddress_Country")] [MaxLength(31)] public string? ShipAddress_Country { get; set; }
    [Column("ShipAddress_Note")] [MaxLength(41)] public string? ShipAddress_Note { get; set; }

    [Column("Phone")] [MaxLength(21)] public string? Phone { get; set; }
    [Column("Mobile")] [MaxLength(21)] public string? Mobile { get; set; }
    [Column("Pager")] [MaxLength(10)] public string? Pager { get; set; }
    [Column("AltPhone")] [MaxLength(21)] public string? AltPhone { get; set; }
    [Column("Fax")] [MaxLength(21)] public string? Fax { get; set; }
    [Column("Email")] [MaxLength(1000)] public string? Email { get; set; }
    [Column("Cc")] [MaxLength(1000)] public string? Cc { get; set; }
    [Column("Contact")] [MaxLength(41)] public string? Contact { get; set; }
    [Column("AltContact")] [MaxLength(41)] public string? AltContact { get; set; }
    [Column("NameOnCheck")] [MaxLength(41)] public string? NameOnCheck { get; set; }
    [Column("Notes")] [MaxLength(1000)] public string? Notes { get; set; }
    [Column("AccountNumber")] [MaxLength(99)] public string? AccountNumber { get; set; }

    [Column("VendorTypeRef_ListID")] [MaxLength(36)] public string? VendorTypeRef_ListID { get; set; }
    [Column("VendorTypeRef_FullName")] [MaxLength(159)] public string? VendorTypeRef_FullName { get; set; }
    [Column("TermsRef_ListID")] [MaxLength(36)] public string? TermsRef_ListID { get; set; }
    [Column("TermsRef_FullName")] [MaxLength(31)] public string? TermsRef_FullName { get; set; }

    [Column("CreditLimit")] public decimal? CreditLimit { get; set; }
    [Column("VendorTaxIdent")] [MaxLength(15)] public string? VendorTaxIdent { get; set; }
    [Column("IsVendorEligibleFor1099")] [MaxLength(5)] public string? IsVendorEligibleFor1099 { get; set; }
    [Column("Balance")] public decimal? Balance { get; set; }

    [Column("CurrencyRef_ListID")] [MaxLength(36)] public string? CurrencyRef_ListID { get; set; }
    [Column("CurrencyRef_FullName")] [MaxLength(64)] public string? CurrencyRef_FullName { get; set; }
    [Column("BillingRateRef_ListID")] [MaxLength(36)] public string? BillingRateRef_ListID { get; set; }
    [Column("BillingRateRef_FullName")] [MaxLength(31)] public string? BillingRateRef_FullName { get; set; }
    [Column("SalesTaxCodeRef_ListID")] [MaxLength(36)] public string? SalesTaxCodeRef_ListID { get; set; }
    [Column("SalesTaxCodeRef_FullName")] [MaxLength(5)] public string? SalesTaxCodeRef_FullName { get; set; }

    [Column("SalesTaxCountry")] [MaxLength(255)] public string? SalesTaxCountry { get; set; }
    [Column("IsSalesTaxAgency")] [MaxLength(5)] public string? IsSalesTaxAgency { get; set; }
    [Column("SalesTaxReturnRef_ListID")] [MaxLength(36)] public string? SalesTaxReturnRef_ListID { get; set; }
    [Column("SalesTaxReturnRef_FullName")] [MaxLength(255)] public string? SalesTaxReturnRef_FullName { get; set; }
    [Column("TaxRegistrationNumber")] [MaxLength(255)] public string? TaxRegistrationNumber { get; set; }
    [Column("ReportingPeriod")] [MaxLength(255)] public string? ReportingPeriod { get; set; }

    [Column("IsTaxTrackedOnPurchases")] [MaxLength(5)] public string? IsTaxTrackedOnPurchases { get; set; }
    [Column("TaxOnPurchasesAccountRef_ListID")] [MaxLength(255)] public string? TaxOnPurchasesAccountRef_ListID { get; set; }
    [Column("TaxOnPurchasesAccountRef_FullName")] [MaxLength(255)] public string? TaxOnPurchasesAccountRef_FullName { get; set; }

    [Column("IsTaxTrackedOnSales")] [MaxLength(5)] public string? IsTaxTrackedOnSales { get; set; }
    [Column("TaxOnSalesAccountRef_ListID")] [MaxLength(36)] public string? TaxOnSalesAccountRef_ListID { get; set; }
    [Column("TaxOnSalesAccountRef_FullName")] [MaxLength(255)] public string? TaxOnSalesAccountRef_FullName { get; set; }
    [Column("IsTaxOnTax")] [MaxLength(5)] public string? IsTaxOnTax { get; set; }

    [Column("PrefillAccountRef_ListID")] [MaxLength(36)] public string? PrefillAccountRef_ListID { get; set; }
    [Column("PrefillAccountRef_FullName")] [MaxLength(255)] public string? PrefillAccountRef_FullName { get; set; }
    [Column("ClassRef_ListID")] [MaxLength(36)] public string? ClassRef_ListID { get; set; }
    [Column("ClassRef_FullName")] [MaxLength(159)] public string? ClassRef_FullName { get; set; }

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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Data;
using InventoryApi.DTOs.Common;
using InventoryApi.DTOs.Vendor;
using InventoryApi.Helpers;
using InventoryApi.Models;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/vendor")]
public class VendorController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public VendorController(InventoryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<VendorResponseDto>>>> GetAll(
        [FromQuery] string? isActive)
    {
        IQueryable<Vendor> query = _db.Vendors;

        if (!string.IsNullOrWhiteSpace(isActive))
            query = query.Where(v => v.IsActive == isActive);

        var items = await query
            .OrderBy(v => v.Name)
            .Select(v => MapToResponse(v))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<VendorResponseDto>>.Ok(items, totalCount: items.Count));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<VendorResponseDto>>> GetById(string id)
    {
        var entity = await _db.Vendors.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<VendorResponseDto>
                .Fail($"Vendor with ListID '{id}' was not found."));

        return Ok(ApiResponse<VendorResponseDto>.Ok(MapToResponse(entity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VendorResponseDto>>> Create(
        [FromBody] VendorCreateDto dto)
    {
        var now = DateTime.UtcNow;

        var entity = new Vendor
        {
            ListID                            = Guid.NewGuid().ToString(),
            TimeCreated                       = now,
            TimeModified                      = now,
            EditSequence                      = EditSequenceHelper.Generate(),
            Name                              = dto.Name,
            IsActive                          = dto.IsActive ?? "true",
            CompanyName                       = dto.CompanyName,
            Salutation                        = dto.Salutation,
            FirstName                         = dto.FirstName,
            MiddleName                        = dto.MiddleName,
            LastName                          = dto.LastName,
            Suffix                            = dto.Suffix,
            JobTitle                          = dto.JobTitle,
            VendorAddress_Addr1               = dto.VendorAddress_Addr1,
            VendorAddress_Addr2               = dto.VendorAddress_Addr2,
            VendorAddress_Addr3               = dto.VendorAddress_Addr3,
            VendorAddress_Addr4               = dto.VendorAddress_Addr4,
            VendorAddress_Addr5               = dto.VendorAddress_Addr5,
            VendorAddress_City                = dto.VendorAddress_City,
            VendorAddress_State               = dto.VendorAddress_State,
            VendorAddress_PostalCode          = dto.VendorAddress_PostalCode,
            VendorAddress_Country             = dto.VendorAddress_Country,
            VendorAddress_Note                = dto.VendorAddress_Note,
            ShipAddress_Addr1                 = dto.ShipAddress_Addr1,
            ShipAddress_Addr2                 = dto.ShipAddress_Addr2,
            ShipAddress_Addr3                 = dto.ShipAddress_Addr3,
            ShipAddress_Addr4                 = dto.ShipAddress_Addr4,
            ShipAddress_Addr5                 = dto.ShipAddress_Addr5,
            ShipAddress_City                  = dto.ShipAddress_City,
            ShipAddress_State                 = dto.ShipAddress_State,
            ShipAddress_PostalCode            = dto.ShipAddress_PostalCode,
            ShipAddress_Country               = dto.ShipAddress_Country,
            ShipAddress_Note                  = dto.ShipAddress_Note,
            Phone                             = dto.Phone,
            Mobile                            = dto.Mobile,
            Pager                             = dto.Pager,
            AltPhone                          = dto.AltPhone,
            Fax                               = dto.Fax,
            Email                             = dto.Email,
            Cc                                = dto.Cc,
            Contact                           = dto.Contact,
            AltContact                        = dto.AltContact,
            NameOnCheck                       = dto.NameOnCheck,
            Notes                             = dto.Notes,
            AccountNumber                     = dto.AccountNumber,
            VendorTypeRef_ListID              = dto.VendorTypeRef_ListID,
            VendorTypeRef_FullName            = dto.VendorTypeRef_FullName,
            TermsRef_ListID                   = dto.TermsRef_ListID,
            TermsRef_FullName                 = dto.TermsRef_FullName,
            CreditLimit                       = dto.CreditLimit,
            VendorTaxIdent                    = dto.VendorTaxIdent,
            IsVendorEligibleFor1099           = dto.IsVendorEligibleFor1099,
            Balance                           = dto.Balance,
            CurrencyRef_ListID                = dto.CurrencyRef_ListID,
            CurrencyRef_FullName              = dto.CurrencyRef_FullName,
            BillingRateRef_ListID             = dto.BillingRateRef_ListID,
            BillingRateRef_FullName           = dto.BillingRateRef_FullName,
            SalesTaxCodeRef_ListID            = dto.SalesTaxCodeRef_ListID,
            SalesTaxCodeRef_FullName          = dto.SalesTaxCodeRef_FullName,
            SalesTaxCountry                   = dto.SalesTaxCountry,
            IsSalesTaxAgency                  = dto.IsSalesTaxAgency,
            SalesTaxReturnRef_ListID          = dto.SalesTaxReturnRef_ListID,
            SalesTaxReturnRef_FullName        = dto.SalesTaxReturnRef_FullName,
            TaxRegistrationNumber             = dto.TaxRegistrationNumber,
            ReportingPeriod                   = dto.ReportingPeriod,
            IsTaxTrackedOnPurchases           = dto.IsTaxTrackedOnPurchases,
            TaxOnPurchasesAccountRef_ListID   = dto.TaxOnPurchasesAccountRef_ListID,
            TaxOnPurchasesAccountRef_FullName = dto.TaxOnPurchasesAccountRef_FullName,
            IsTaxTrackedOnSales               = dto.IsTaxTrackedOnSales,
            TaxOnSalesAccountRef_ListID       = dto.TaxOnSalesAccountRef_ListID,
            TaxOnSalesAccountRef_FullName     = dto.TaxOnSalesAccountRef_FullName,
            IsTaxOnTax                        = dto.IsTaxOnTax,
            PrefillAccountRef_ListID          = dto.PrefillAccountRef_ListID,
            PrefillAccountRef_FullName        = dto.PrefillAccountRef_FullName,
            ClassRef_ListID                   = dto.ClassRef_ListID,
            ClassRef_FullName                 = dto.ClassRef_FullName,
            UserData                          = dto.UserData,
            CustomField1                      = dto.CustomField1,
            CustomField2                      = dto.CustomField2,
            CustomField3                      = dto.CustomField3,
            CustomField4                      = dto.CustomField4,
            CustomField5                      = dto.CustomField5,
            CustomField6                      = dto.CustomField6,
            CustomField7                      = dto.CustomField7,
            CustomField8                      = dto.CustomField8,
            CustomField9                      = dto.CustomField9,
            CustomField10                     = dto.CustomField10,
            CustomField11                     = dto.CustomField11,
            CustomField12                     = dto.CustomField12,
            CustomField13                     = dto.CustomField13,
            CustomField14                     = dto.CustomField14,
            CustomField15                     = dto.CustomField15,
            Status                            = dto.Status,
            ExternalGUID                      = dto.ExternalGUID
        };

        _db.Vendors.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.ListID },
            ApiResponse<VendorResponseDto>.Ok(MapToResponse(entity), "Created successfully."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<VendorResponseDto>>> Update(
        string id, [FromBody] VendorUpdateDto dto)
    {
        var entity = await _db.Vendors.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<VendorResponseDto>
                .Fail($"Vendor with ListID '{id}' was not found."));

        entity.TimeModified                      = DateTime.UtcNow;
        entity.EditSequence                      = EditSequenceHelper.Increment(entity.EditSequence);
        entity.Name                              = dto.Name;
        entity.IsActive                          = dto.IsActive;
        entity.CompanyName                       = dto.CompanyName;
        entity.Salutation                        = dto.Salutation;
        entity.FirstName                         = dto.FirstName;
        entity.MiddleName                        = dto.MiddleName;
        entity.LastName                          = dto.LastName;
        entity.Suffix                            = dto.Suffix;
        entity.JobTitle                          = dto.JobTitle;
        entity.VendorAddress_Addr1               = dto.VendorAddress_Addr1;
        entity.VendorAddress_Addr2               = dto.VendorAddress_Addr2;
        entity.VendorAddress_Addr3               = dto.VendorAddress_Addr3;
        entity.VendorAddress_Addr4               = dto.VendorAddress_Addr4;
        entity.VendorAddress_Addr5               = dto.VendorAddress_Addr5;
        entity.VendorAddress_City                = dto.VendorAddress_City;
        entity.VendorAddress_State               = dto.VendorAddress_State;
        entity.VendorAddress_PostalCode          = dto.VendorAddress_PostalCode;
        entity.VendorAddress_Country             = dto.VendorAddress_Country;
        entity.VendorAddress_Note                = dto.VendorAddress_Note;
        entity.ShipAddress_Addr1                 = dto.ShipAddress_Addr1;
        entity.ShipAddress_Addr2                 = dto.ShipAddress_Addr2;
        entity.ShipAddress_Addr3                 = dto.ShipAddress_Addr3;
        entity.ShipAddress_Addr4                 = dto.ShipAddress_Addr4;
        entity.ShipAddress_Addr5                 = dto.ShipAddress_Addr5;
        entity.ShipAddress_City                  = dto.ShipAddress_City;
        entity.ShipAddress_State                 = dto.ShipAddress_State;
        entity.ShipAddress_PostalCode            = dto.ShipAddress_PostalCode;
        entity.ShipAddress_Country               = dto.ShipAddress_Country;
        entity.ShipAddress_Note                  = dto.ShipAddress_Note;
        entity.Phone                             = dto.Phone;
        entity.Mobile                            = dto.Mobile;
        entity.Pager                             = dto.Pager;
        entity.AltPhone                          = dto.AltPhone;
        entity.Fax                               = dto.Fax;
        entity.Email                             = dto.Email;
        entity.Cc                                = dto.Cc;
        entity.Contact                           = dto.Contact;
        entity.AltContact                        = dto.AltContact;
        entity.NameOnCheck                       = dto.NameOnCheck;
        entity.Notes                             = dto.Notes;
        entity.AccountNumber                     = dto.AccountNumber;
        entity.VendorTypeRef_ListID              = dto.VendorTypeRef_ListID;
        entity.VendorTypeRef_FullName            = dto.VendorTypeRef_FullName;
        entity.TermsRef_ListID                   = dto.TermsRef_ListID;
        entity.TermsRef_FullName                 = dto.TermsRef_FullName;
        entity.CreditLimit                       = dto.CreditLimit;
        entity.VendorTaxIdent                    = dto.VendorTaxIdent;
        entity.IsVendorEligibleFor1099           = dto.IsVendorEligibleFor1099;
        entity.Balance                           = dto.Balance;
        entity.CurrencyRef_ListID                = dto.CurrencyRef_ListID;
        entity.CurrencyRef_FullName              = dto.CurrencyRef_FullName;
        entity.BillingRateRef_ListID             = dto.BillingRateRef_ListID;
        entity.BillingRateRef_FullName           = dto.BillingRateRef_FullName;
        entity.SalesTaxCodeRef_ListID            = dto.SalesTaxCodeRef_ListID;
        entity.SalesTaxCodeRef_FullName          = dto.SalesTaxCodeRef_FullName;
        entity.SalesTaxCountry                   = dto.SalesTaxCountry;
        entity.IsSalesTaxAgency                  = dto.IsSalesTaxAgency;
        entity.SalesTaxReturnRef_ListID          = dto.SalesTaxReturnRef_ListID;
        entity.SalesTaxReturnRef_FullName        = dto.SalesTaxReturnRef_FullName;
        entity.TaxRegistrationNumber             = dto.TaxRegistrationNumber;
        entity.ReportingPeriod                   = dto.ReportingPeriod;
        entity.IsTaxTrackedOnPurchases           = dto.IsTaxTrackedOnPurchases;
        entity.TaxOnPurchasesAccountRef_ListID   = dto.TaxOnPurchasesAccountRef_ListID;
        entity.TaxOnPurchasesAccountRef_FullName = dto.TaxOnPurchasesAccountRef_FullName;
        entity.IsTaxTrackedOnSales               = dto.IsTaxTrackedOnSales;
        entity.TaxOnSalesAccountRef_ListID       = dto.TaxOnSalesAccountRef_ListID;
        entity.TaxOnSalesAccountRef_FullName     = dto.TaxOnSalesAccountRef_FullName;
        entity.IsTaxOnTax                        = dto.IsTaxOnTax;
        entity.PrefillAccountRef_ListID          = dto.PrefillAccountRef_ListID;
        entity.PrefillAccountRef_FullName        = dto.PrefillAccountRef_FullName;
        entity.ClassRef_ListID                   = dto.ClassRef_ListID;
        entity.ClassRef_FullName                 = dto.ClassRef_FullName;
        entity.UserData                          = dto.UserData;
        entity.CustomField1                      = dto.CustomField1;
        entity.CustomField2                      = dto.CustomField2;
        entity.CustomField3                      = dto.CustomField3;
        entity.CustomField4                      = dto.CustomField4;
        entity.CustomField5                      = dto.CustomField5;
        entity.CustomField6                      = dto.CustomField6;
        entity.CustomField7                      = dto.CustomField7;
        entity.CustomField8                      = dto.CustomField8;
        entity.CustomField9                      = dto.CustomField9;
        entity.CustomField10                     = dto.CustomField10;
        entity.CustomField11                     = dto.CustomField11;
        entity.CustomField12                     = dto.CustomField12;
        entity.CustomField13                     = dto.CustomField13;
        entity.CustomField14                     = dto.CustomField14;
        entity.CustomField15                     = dto.CustomField15;
        entity.Status                            = dto.Status;
        entity.ExternalGUID                      = dto.ExternalGUID;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<VendorResponseDto>.Ok(MapToResponse(entity), "Updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var entity = await _db.Vendors.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<object>.Fail($"Vendor with ListID '{id}' was not found."));

        entity.IsActive     = "false";
        entity.TimeModified = DateTime.UtcNow;
        entity.EditSequence = EditSequenceHelper.Increment(entity.EditSequence);

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Record deactivated successfully."));
    }

    private static VendorResponseDto MapToResponse(Vendor v) => new()
    {
        ListID                            = v.ListID,
        TimeCreated                       = v.TimeCreated,
        TimeModified                      = v.TimeModified,
        EditSequence                      = v.EditSequence,
        Name                              = v.Name,
        IsActive                          = v.IsActive,
        CompanyName                       = v.CompanyName,
        Salutation                        = v.Salutation,
        FirstName                         = v.FirstName,
        MiddleName                        = v.MiddleName,
        LastName                          = v.LastName,
        Suffix                            = v.Suffix,
        JobTitle                          = v.JobTitle,
        VendorAddress_Addr1               = v.VendorAddress_Addr1,
        VendorAddress_Addr2               = v.VendorAddress_Addr2,
        VendorAddress_Addr3               = v.VendorAddress_Addr3,
        VendorAddress_Addr4               = v.VendorAddress_Addr4,
        VendorAddress_Addr5               = v.VendorAddress_Addr5,
        VendorAddress_City                = v.VendorAddress_City,
        VendorAddress_State               = v.VendorAddress_State,
        VendorAddress_PostalCode          = v.VendorAddress_PostalCode,
        VendorAddress_Country             = v.VendorAddress_Country,
        VendorAddress_Note                = v.VendorAddress_Note,
        ShipAddress_Addr1                 = v.ShipAddress_Addr1,
        ShipAddress_Addr2                 = v.ShipAddress_Addr2,
        ShipAddress_Addr3                 = v.ShipAddress_Addr3,
        ShipAddress_Addr4                 = v.ShipAddress_Addr4,
        ShipAddress_Addr5                 = v.ShipAddress_Addr5,
        ShipAddress_City                  = v.ShipAddress_City,
        ShipAddress_State                 = v.ShipAddress_State,
        ShipAddress_PostalCode            = v.ShipAddress_PostalCode,
        ShipAddress_Country               = v.ShipAddress_Country,
        ShipAddress_Note                  = v.ShipAddress_Note,
        Phone                             = v.Phone,
        Mobile                            = v.Mobile,
        Pager                             = v.Pager,
        AltPhone                          = v.AltPhone,
        Fax                               = v.Fax,
        Email                             = v.Email,
        Cc                                = v.Cc,
        Contact                           = v.Contact,
        AltContact                        = v.AltContact,
        NameOnCheck                       = v.NameOnCheck,
        Notes                             = v.Notes,
        AccountNumber                     = v.AccountNumber,
        VendorTypeRef_ListID              = v.VendorTypeRef_ListID,
        VendorTypeRef_FullName            = v.VendorTypeRef_FullName,
        TermsRef_ListID                   = v.TermsRef_ListID,
        TermsRef_FullName                 = v.TermsRef_FullName,
        CreditLimit                       = v.CreditLimit,
        VendorTaxIdent                    = v.VendorTaxIdent,
        IsVendorEligibleFor1099           = v.IsVendorEligibleFor1099,
        Balance                           = v.Balance,
        CurrencyRef_ListID                = v.CurrencyRef_ListID,
        CurrencyRef_FullName              = v.CurrencyRef_FullName,
        BillingRateRef_ListID             = v.BillingRateRef_ListID,
        BillingRateRef_FullName           = v.BillingRateRef_FullName,
        SalesTaxCodeRef_ListID            = v.SalesTaxCodeRef_ListID,
        SalesTaxCodeRef_FullName          = v.SalesTaxCodeRef_FullName,
        SalesTaxCountry                   = v.SalesTaxCountry,
        IsSalesTaxAgency                  = v.IsSalesTaxAgency,
        SalesTaxReturnRef_ListID          = v.SalesTaxReturnRef_ListID,
        SalesTaxReturnRef_FullName        = v.SalesTaxReturnRef_FullName,
        TaxRegistrationNumber             = v.TaxRegistrationNumber,
        ReportingPeriod                   = v.ReportingPeriod,
        IsTaxTrackedOnPurchases           = v.IsTaxTrackedOnPurchases,
        TaxOnPurchasesAccountRef_ListID   = v.TaxOnPurchasesAccountRef_ListID,
        TaxOnPurchasesAccountRef_FullName = v.TaxOnPurchasesAccountRef_FullName,
        IsTaxTrackedOnSales               = v.IsTaxTrackedOnSales,
        TaxOnSalesAccountRef_ListID       = v.TaxOnSalesAccountRef_ListID,
        TaxOnSalesAccountRef_FullName     = v.TaxOnSalesAccountRef_FullName,
        IsTaxOnTax                        = v.IsTaxOnTax,
        PrefillAccountRef_ListID          = v.PrefillAccountRef_ListID,
        PrefillAccountRef_FullName        = v.PrefillAccountRef_FullName,
        ClassRef_ListID                   = v.ClassRef_ListID,
        ClassRef_FullName                 = v.ClassRef_FullName,
        UserData                          = v.UserData,
        CustomField1                      = v.CustomField1,
        CustomField2                      = v.CustomField2,
        CustomField3                      = v.CustomField3,
        CustomField4                      = v.CustomField4,
        CustomField5                      = v.CustomField5,
        CustomField6                      = v.CustomField6,
        CustomField7                      = v.CustomField7,
        CustomField8                      = v.CustomField8,
        CustomField9                      = v.CustomField9,
        CustomField10                     = v.CustomField10,
        CustomField11                     = v.CustomField11,
        CustomField12                     = v.CustomField12,
        CustomField13                     = v.CustomField13,
        CustomField14                     = v.CustomField14,
        CustomField15                     = v.CustomField15,
        Status                            = v.Status,
        ExternalGUID                      = v.ExternalGUID
    };
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Data;
using InventoryApi.DTOs.Common;
using InventoryApi.DTOs.InventorySite;
using InventoryApi.Helpers;
using InventoryApi.Models;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/inventorysite")]
public class InventorySiteController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public InventorySiteController(InventoryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventorySiteResponseDto>>>> GetAll(
        [FromQuery] string? isActive)
    {
        IQueryable<InventorySite> query = _db.InventorySites;

        if (!string.IsNullOrWhiteSpace(isActive))
            query = query.Where(s => s.IsActive == isActive);

        var items = await query
            .OrderBy(s => s.Name)
            .Select(s => MapToResponse(s))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<InventorySiteResponseDto>>.Ok(items, totalCount: items.Count));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InventorySiteResponseDto>>> GetById(string id)
    {
        var entity = await _db.InventorySites.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<InventorySiteResponseDto>
                .Fail($"InventorySite with ListID '{id}' was not found."));

        return Ok(ApiResponse<InventorySiteResponseDto>.Ok(MapToResponse(entity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InventorySiteResponseDto>>> Create(
        [FromBody] InventorySiteCreateDto dto)
    {
        var now = DateTime.UtcNow;

        var entity = new InventorySite
        {
            ListID                  = Guid.NewGuid().ToString(),
            TimeCreated             = now,
            TimeModified            = now,
            EditSequence            = EditSequenceHelper.Generate(),
            Name                    = dto.Name,
            IsActive                = dto.IsActive ?? "true",
            ParentSiteRef_ListID    = dto.ParentSiteRef_ListID,
            ParentSiteRef_FullName  = dto.ParentSiteRef_FullName,
            IsDefaultSite           = dto.IsDefaultSite ?? "false",
            SiteDesc                = dto.SiteDesc,
            Contact                 = dto.Contact,
            Phone                   = dto.Phone,
            Fax                     = dto.Fax,
            Email                   = dto.Email,
            SiteAddress_Addr1       = dto.SiteAddress_Addr1,
            SiteAddress_Addr2       = dto.SiteAddress_Addr2,
            SiteAddress_Addr3       = dto.SiteAddress_Addr3,
            SiteAddress_Addr4       = dto.SiteAddress_Addr4,
            SiteAddress_Addr5       = dto.SiteAddress_Addr5,
            SiteAddress_City        = dto.SiteAddress_City,
            SiteAddress_State       = dto.SiteAddress_State,
            SiteAddress_PostalCode  = dto.SiteAddress_PostalCode,
            SiteAddress_Country     = dto.SiteAddress_Country,
            SiteAddressBlock_Addr1  = dto.SiteAddressBlock_Addr1,
            SiteAddressBlock_Addr2  = dto.SiteAddressBlock_Addr2,
            SiteAddressBlock_Addr3  = dto.SiteAddressBlock_Addr3,
            SiteAddressBlock_Addr4  = dto.SiteAddressBlock_Addr4,
            SiteAddressBlock_Addr5  = dto.SiteAddressBlock_Addr5,
            UserData                = dto.UserData,
            Status                  = dto.Status
        };

        _db.InventorySites.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.ListID },
            ApiResponse<InventorySiteResponseDto>.Ok(MapToResponse(entity), "Created successfully."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<InventorySiteResponseDto>>> Update(
        string id, [FromBody] InventorySiteUpdateDto dto)
    {
        var entity = await _db.InventorySites.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<InventorySiteResponseDto>
                .Fail($"InventorySite with ListID '{id}' was not found."));

        entity.TimeModified            = DateTime.UtcNow;
        entity.EditSequence            = EditSequenceHelper.Increment(entity.EditSequence);
        entity.Name                    = dto.Name;
        entity.IsActive                = dto.IsActive;
        entity.ParentSiteRef_ListID    = dto.ParentSiteRef_ListID;
        entity.ParentSiteRef_FullName  = dto.ParentSiteRef_FullName;
        entity.IsDefaultSite           = dto.IsDefaultSite;
        entity.SiteDesc                = dto.SiteDesc;
        entity.Contact                 = dto.Contact;
        entity.Phone                   = dto.Phone;
        entity.Fax                     = dto.Fax;
        entity.Email                   = dto.Email;
        entity.SiteAddress_Addr1       = dto.SiteAddress_Addr1;
        entity.SiteAddress_Addr2       = dto.SiteAddress_Addr2;
        entity.SiteAddress_Addr3       = dto.SiteAddress_Addr3;
        entity.SiteAddress_Addr4       = dto.SiteAddress_Addr4;
        entity.SiteAddress_Addr5       = dto.SiteAddress_Addr5;
        entity.SiteAddress_City        = dto.SiteAddress_City;
        entity.SiteAddress_State       = dto.SiteAddress_State;
        entity.SiteAddress_PostalCode  = dto.SiteAddress_PostalCode;
        entity.SiteAddress_Country     = dto.SiteAddress_Country;
        entity.SiteAddressBlock_Addr1  = dto.SiteAddressBlock_Addr1;
        entity.SiteAddressBlock_Addr2  = dto.SiteAddressBlock_Addr2;
        entity.SiteAddressBlock_Addr3  = dto.SiteAddressBlock_Addr3;
        entity.SiteAddressBlock_Addr4  = dto.SiteAddressBlock_Addr4;
        entity.SiteAddressBlock_Addr5  = dto.SiteAddressBlock_Addr5;
        entity.UserData                = dto.UserData;
        entity.Status                  = dto.Status;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<InventorySiteResponseDto>.Ok(MapToResponse(entity), "Updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var entity = await _db.InventorySites.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<object>.Fail($"InventorySite with ListID '{id}' was not found."));

        entity.IsActive     = "false";
        entity.TimeModified = DateTime.UtcNow;
        entity.EditSequence = EditSequenceHelper.Increment(entity.EditSequence);

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Record deactivated successfully."));
    }

    private static InventorySiteResponseDto MapToResponse(InventorySite s) => new()
    {
        ListID                  = s.ListID,
        TimeCreated             = s.TimeCreated,
        TimeModified            = s.TimeModified,
        EditSequence            = s.EditSequence,
        Name                    = s.Name,
        IsActive                = s.IsActive,
        ParentSiteRef_ListID    = s.ParentSiteRef_ListID,
        ParentSiteRef_FullName  = s.ParentSiteRef_FullName,
        IsDefaultSite           = s.IsDefaultSite,
        SiteDesc                = s.SiteDesc,
        Contact                 = s.Contact,
        Phone                   = s.Phone,
        Fax                     = s.Fax,
        Email                   = s.Email,
        SiteAddress_Addr1       = s.SiteAddress_Addr1,
        SiteAddress_Addr2       = s.SiteAddress_Addr2,
        SiteAddress_Addr3       = s.SiteAddress_Addr3,
        SiteAddress_Addr4       = s.SiteAddress_Addr4,
        SiteAddress_Addr5       = s.SiteAddress_Addr5,
        SiteAddress_City        = s.SiteAddress_City,
        SiteAddress_State       = s.SiteAddress_State,
        SiteAddress_PostalCode  = s.SiteAddress_PostalCode,
        SiteAddress_Country     = s.SiteAddress_Country,
        SiteAddressBlock_Addr1  = s.SiteAddressBlock_Addr1,
        SiteAddressBlock_Addr2  = s.SiteAddressBlock_Addr2,
        SiteAddressBlock_Addr3  = s.SiteAddressBlock_Addr3,
        SiteAddressBlock_Addr4  = s.SiteAddressBlock_Addr4,
        SiteAddressBlock_Addr5  = s.SiteAddressBlock_Addr5,
        UserData                = s.UserData,
        Status                  = s.Status
    };
}

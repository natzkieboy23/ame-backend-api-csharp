using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Data;
using InventoryApi.DTOs.Common;
using InventoryApi.DTOs.PriceLevel;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/pricelevel")]
public class PriceLevelController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public PriceLevelController(InventoryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PriceLevelResponseDto>>>> GetAll(
        [FromQuery] string? isActive)
    {
        IQueryable<Models.PriceLevel> query = _db.PriceLevels.Include(p => p.PerItemDetails);

        if (!string.IsNullOrWhiteSpace(isActive))
            query = query.Where(p => p.IsActive == isActive);

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => MapToResponse(p))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PriceLevelResponseDto>>.Ok(items, totalCount: items.Count));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PriceLevelResponseDto>>> GetById(string id)
    {
        var entity = await _db.PriceLevels
            .Include(p => p.PerItemDetails)
            .FirstOrDefaultAsync(p => p.ListID == id);

        if (entity is null)
            return NotFound(ApiResponse<PriceLevelResponseDto>
                .Fail($"PriceLevel with ListID '{id}' was not found."));

        return Ok(ApiResponse<PriceLevelResponseDto>.Ok(MapToResponse(entity)));
    }

    private static PriceLevelResponseDto MapToResponse(Models.PriceLevel p) => new()
    {
        ListID                    = p.ListID,
        TimeCreated               = p.TimeCreated,
        TimeModified              = p.TimeModified,
        EditSequence              = p.EditSequence,
        Name                      = p.Name,
        IsActive                  = p.IsActive,
        PriceLevelType            = p.PriceLevelType,
        PriceLevelFixedPercentage = p.PriceLevelFixedPercentage,
        CurrencyRef_ListID        = p.CurrencyRef_ListID,
        CurrencyRef_FullName      = p.CurrencyRef_FullName,
        UserData                  = p.UserData,
        Status                    = p.Status,
        PerItemDetails            = p.PerItemDetails.Select(d => new PriceLevelPerItemResponseDto
        {
            ItemRef_ListID     = d.ItemRef_ListID,
            ItemRef_FullName   = d.ItemRef_FullName,
            CustomPrice        = d.CustomPrice,
            CustomPricePercent = d.CustomPricePercent,
            AdjustPercentage   = d.AdjustPercentage,
            AdjustRelativeTo   = d.AdjustRelativeTo,
        })
    };
}

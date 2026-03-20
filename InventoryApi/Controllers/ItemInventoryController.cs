using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Data;
using InventoryApi.DTOs.Common;
using InventoryApi.DTOs.ItemInventory;
using InventoryApi.Helpers;
using InventoryApi.Models;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/iteminventory")]
public class ItemInventoryController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public ItemInventoryController(InventoryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItemInventoryResponseDto>>>> GetAll(
        [FromQuery] string? isActive,
        [FromQuery] string? manufacturerPartNumber,
        [FromQuery] string? siteFullName)
    {
        IQueryable<ItemInventory> query = _db.ItemInventories;

        if (!string.IsNullOrWhiteSpace(isActive))
            query = query.Where(i => i.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(manufacturerPartNumber))
            query = query.Where(i => i.ManufacturerPartNumber == manufacturerPartNumber);

        var warehousePrices = await GetWarehousePriceMapAsync();

        if (!string.IsNullOrWhiteSpace(siteFullName))
        {
            var allItems = await query.OrderBy(i => i.Name).ToListAsync();

            // Fetch site quantities separately and sum across sub-locations
            var siteItems = await _db.ItemSites
                .Where(s => s.InventorySiteRef_FullName == siteFullName
                         && s.ItemInventoryRef_ListID != null)
                .ToListAsync();

            var siteMap = siteItems
                .GroupBy(s => s.ItemInventoryRef_ListID!)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityOnHand ?? 0m));

            var dtos = allItems.Select(i =>
            {
                var dto = MapToResponse(i);
                dto.SiteQuantityOnHand = siteMap.TryGetValue(i.ListID, out var qty) ? qty : null;
                dto.WarehousePrice     = warehousePrices.TryGetValue(i.ListID, out var wp) ? wp : 0m;
                return dto;
            }).ToList();

            return Ok(ApiResponse<IEnumerable<ItemInventoryResponseDto>>.Ok(dtos, totalCount: dtos.Count));
        }

        var items = await query
            .OrderBy(i => i.Name)
            .ToListAsync();

        var result = items.Select(i =>
        {
            var dto = MapToResponse(i);
            dto.WarehousePrice = warehousePrices.TryGetValue(i.ListID, out var wp) ? wp : 0m;
            return dto;
        }).ToList();

        return Ok(ApiResponse<IEnumerable<ItemInventoryResponseDto>>.Ok(result, totalCount: result.Count));
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItemInventoryResponseDto>>>> GetProducts(
        [FromQuery] string? isActive)
    {
        IQueryable<ItemInventory> query = _db.ItemInventories;

        if (!string.IsNullOrWhiteSpace(isActive))
            query = query.Where(i => i.IsActive == isActive);

        var items = await query.OrderBy(i => i.Name).ToListAsync();

        var siteItems = await _db.ItemSites
            .Where(s => s.InventorySiteRef_FullName == "Warehouse"
                     && s.ItemInventoryRef_ListID != null)
            .ToListAsync();

        var siteMap = siteItems
            .GroupBy(s => s.ItemInventoryRef_ListID!)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityOnHand ?? 0m));

        var warehousePrices = await GetWarehousePriceMapAsync();

        var result = items.Select(i =>
        {
            var dto = MapToResponse(i);
            dto.SiteQuantityOnHand = siteMap.TryGetValue(i.ListID, out var qty) ? qty : 0m;
            dto.WarehousePrice     = warehousePrices.TryGetValue(i.ListID, out var wp) ? wp : 0m;
            return dto;
        }).ToList();

        return Ok(ApiResponse<IEnumerable<ItemInventoryResponseDto>>.Ok(result, totalCount: result.Count));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ItemInventoryResponseDto>>> GetById(string id)
    {
        var entity = await _db.ItemInventories.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<ItemInventoryResponseDto>
                .Fail($"ItemInventory with ListID '{id}' was not found."));

        var warehousePrices = await GetWarehousePriceMapAsync();
        var dto = MapToResponse(entity);
        dto.WarehousePrice = warehousePrices.TryGetValue(entity.ListID, out var wp) ? wp : 0m;

        return Ok(ApiResponse<ItemInventoryResponseDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemInventoryResponseDto>>> Create(
        [FromBody] ItemInventoryCreateDto dto)
    {
        var now = DateTime.UtcNow;

        var entity = new ItemInventory
        {
            ListID                        = Guid.NewGuid().ToString(),
            TimeCreated                   = now,
            TimeModified                  = now,
            EditSequence                  = EditSequenceHelper.Generate(),
            Name                          = dto.Name,
            FullName                      = dto.FullName,
            BarCodeValue                  = dto.BarCodeValue,
            IsActive                      = dto.IsActive ?? "true",
            ClassRef_ListID               = dto.ClassRef_ListID,
            ClassRef_FullName             = dto.ClassRef_FullName,
            ParentRef_ListID              = dto.ParentRef_ListID,
            ParentRef_FullName            = dto.ParentRef_FullName,
            Sublevel                      = dto.Sublevel,
            ManufacturerPartNumber        = dto.ManufacturerPartNumber,
            UnitOfMeasureSetRef_ListID    = dto.UnitOfMeasureSetRef_ListID,
            UnitOfMeasureSetRef_FullName  = dto.UnitOfMeasureSetRef_FullName,
            IsTaxIncluded                 = dto.IsTaxIncluded,
            SalesTaxCodeRef_ListID        = dto.SalesTaxCodeRef_ListID,
            SalesTaxCodeRef_FullName      = dto.SalesTaxCodeRef_FullName,
            SalesDesc                     = dto.SalesDesc,
            SalesPrice                    = dto.SalesPrice,
            IncomeAccountRef_ListID       = dto.IncomeAccountRef_ListID,
            IncomeAccountRef_FullName     = dto.IncomeAccountRef_FullName,
            PurchaseDesc                  = dto.PurchaseDesc,
            PurchaseCost                  = dto.PurchaseCost,
            COGSAccountRef_ListID         = dto.COGSAccountRef_ListID,
            COGSAccountRef_FullName       = dto.COGSAccountRef_FullName,
            PrefVendorRef_ListID          = dto.PrefVendorRef_ListID,
            PrefVendorRef_FullName        = dto.PrefVendorRef_FullName,
            AssetAccountRef_ListID        = dto.AssetAccountRef_ListID,
            AssetAccountRef_FullName      = dto.AssetAccountRef_FullName,
            ReorderPoint                  = dto.ReorderPoint,
            Max                           = dto.Max,
            QuantityOnHand                = dto.QuantityOnHand,
            AverageCost                   = dto.AverageCost,
            QuantityOnOrder               = dto.QuantityOnOrder,
            QuantityOnSalesOrder          = dto.QuantityOnSalesOrder,
            UserData                      = dto.UserData,
            CustomField1                  = dto.CustomField1,
            CustomField2                  = dto.CustomField2,
            CustomField3                  = dto.CustomField3,
            CustomField4                  = dto.CustomField4,
            CustomField5                  = dto.CustomField5,
            CustomField6                  = dto.CustomField6,
            CustomField7                  = dto.CustomField7,
            CustomField8                  = dto.CustomField8,
            CustomField9                  = dto.CustomField9,
            CustomField10                 = dto.CustomField10,
            CustomField11                 = dto.CustomField11,
            CustomField12                 = dto.CustomField12,
            CustomField13                 = dto.CustomField13,
            CustomField14                 = dto.CustomField14,
            CustomField15                 = dto.CustomField15,
            Status                        = dto.Status,
            ExternalGUID                  = dto.ExternalGUID
        };

        _db.ItemInventories.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.ListID },
            ApiResponse<ItemInventoryResponseDto>.Ok(MapToResponse(entity), "Created successfully."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ItemInventoryResponseDto>>> Update(
        string id, [FromBody] ItemInventoryUpdateDto dto)
    {
        var entity = await _db.ItemInventories.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<ItemInventoryResponseDto>
                .Fail($"ItemInventory with ListID '{id}' was not found."));

        entity.TimeModified                  = DateTime.UtcNow;
        entity.EditSequence                  = EditSequenceHelper.Increment(entity.EditSequence);
        entity.Name                          = dto.Name;
        entity.FullName                      = dto.FullName;
        entity.BarCodeValue                  = dto.BarCodeValue;
        entity.IsActive                      = dto.IsActive;
        entity.ClassRef_ListID               = dto.ClassRef_ListID;
        entity.ClassRef_FullName             = dto.ClassRef_FullName;
        entity.ParentRef_ListID              = dto.ParentRef_ListID;
        entity.ParentRef_FullName            = dto.ParentRef_FullName;
        entity.Sublevel                      = dto.Sublevel;
        entity.ManufacturerPartNumber        = dto.ManufacturerPartNumber;
        entity.UnitOfMeasureSetRef_ListID    = dto.UnitOfMeasureSetRef_ListID;
        entity.UnitOfMeasureSetRef_FullName  = dto.UnitOfMeasureSetRef_FullName;
        entity.IsTaxIncluded                 = dto.IsTaxIncluded;
        entity.SalesTaxCodeRef_ListID        = dto.SalesTaxCodeRef_ListID;
        entity.SalesTaxCodeRef_FullName      = dto.SalesTaxCodeRef_FullName;
        entity.SalesDesc                     = dto.SalesDesc;
        entity.SalesPrice                    = dto.SalesPrice;
        entity.IncomeAccountRef_ListID       = dto.IncomeAccountRef_ListID;
        entity.IncomeAccountRef_FullName     = dto.IncomeAccountRef_FullName;
        entity.PurchaseDesc                  = dto.PurchaseDesc;
        entity.PurchaseCost                  = dto.PurchaseCost;
        entity.COGSAccountRef_ListID         = dto.COGSAccountRef_ListID;
        entity.COGSAccountRef_FullName       = dto.COGSAccountRef_FullName;
        entity.PrefVendorRef_ListID          = dto.PrefVendorRef_ListID;
        entity.PrefVendorRef_FullName        = dto.PrefVendorRef_FullName;
        entity.AssetAccountRef_ListID        = dto.AssetAccountRef_ListID;
        entity.AssetAccountRef_FullName      = dto.AssetAccountRef_FullName;
        entity.ReorderPoint                  = dto.ReorderPoint;
        entity.Max                           = dto.Max;
        entity.QuantityOnHand                = dto.QuantityOnHand;
        entity.AverageCost                   = dto.AverageCost;
        entity.QuantityOnOrder               = dto.QuantityOnOrder;
        entity.QuantityOnSalesOrder          = dto.QuantityOnSalesOrder;
        entity.UserData                      = dto.UserData;
        entity.CustomField1                  = dto.CustomField1;
        entity.CustomField2                  = dto.CustomField2;
        entity.CustomField3                  = dto.CustomField3;
        entity.CustomField4                  = dto.CustomField4;
        entity.CustomField5                  = dto.CustomField5;
        entity.CustomField6                  = dto.CustomField6;
        entity.CustomField7                  = dto.CustomField7;
        entity.CustomField8                  = dto.CustomField8;
        entity.CustomField9                  = dto.CustomField9;
        entity.CustomField10                 = dto.CustomField10;
        entity.CustomField11                 = dto.CustomField11;
        entity.CustomField12                 = dto.CustomField12;
        entity.CustomField13                 = dto.CustomField13;
        entity.CustomField14                 = dto.CustomField14;
        entity.CustomField15                 = dto.CustomField15;
        entity.Status                        = dto.Status;
        entity.ExternalGUID                  = dto.ExternalGUID;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<ItemInventoryResponseDto>.Ok(MapToResponse(entity), "Updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var entity = await _db.ItemInventories.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<object>.Fail($"ItemInventory with ListID '{id}' was not found."));

        entity.IsActive     = "false";
        entity.TimeModified = DateTime.UtcNow;
        entity.EditSequence = EditSequenceHelper.Increment(entity.EditSequence);

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Record deactivated successfully."));
    }

    private async Task<Dictionary<string, decimal>> GetWarehousePriceMapAsync()
    {
        var warehouseIds = await _db.PriceLevels
            .Where(pl => pl.Name!.StartsWith("W"))
            .Select(pl => pl.ListID)
            .ToListAsync();

        if (warehouseIds.Count == 0)
            return [];

        return await _db.PriceLevelPerItemDetails
            .Where(d => warehouseIds.Contains(d.IDKEY) && d.ItemRef_ListID != null)
            .ToDictionaryAsync(d => d.ItemRef_ListID!, d => d.CustomPrice ?? 0m);
    }

    private static ItemInventoryResponseDto MapToResponse(ItemInventory i) => new()
    {
        ListID                        = i.ListID,
        TimeCreated                   = i.TimeCreated,
        TimeModified                  = i.TimeModified,
        EditSequence                  = i.EditSequence,
        Name                          = i.Name,
        FullName                      = i.FullName,
        BarCodeValue                  = i.BarCodeValue,
        IsActive                      = i.IsActive,
        ClassRef_ListID               = i.ClassRef_ListID,
        ClassRef_FullName             = i.ClassRef_FullName,
        ParentRef_ListID              = i.ParentRef_ListID,
        ParentRef_FullName            = i.ParentRef_FullName,
        Sublevel                      = i.Sublevel,
        ManufacturerPartNumber        = i.ManufacturerPartNumber,
        UnitOfMeasureSetRef_ListID    = i.UnitOfMeasureSetRef_ListID,
        UnitOfMeasureSetRef_FullName  = i.UnitOfMeasureSetRef_FullName,
        IsTaxIncluded                 = i.IsTaxIncluded,
        SalesTaxCodeRef_ListID        = i.SalesTaxCodeRef_ListID,
        SalesTaxCodeRef_FullName      = i.SalesTaxCodeRef_FullName,
        SalesDesc                     = i.SalesDesc,
        SalesPrice                    = i.SalesPrice,
        IncomeAccountRef_ListID       = i.IncomeAccountRef_ListID,
        IncomeAccountRef_FullName     = i.IncomeAccountRef_FullName,
        PurchaseDesc                  = i.PurchaseDesc,
        PurchaseCost                  = i.PurchaseCost,
        COGSAccountRef_ListID         = i.COGSAccountRef_ListID,
        COGSAccountRef_FullName       = i.COGSAccountRef_FullName,
        PrefVendorRef_ListID          = i.PrefVendorRef_ListID,
        PrefVendorRef_FullName        = i.PrefVendorRef_FullName,
        AssetAccountRef_ListID        = i.AssetAccountRef_ListID,
        AssetAccountRef_FullName      = i.AssetAccountRef_FullName,
        ReorderPoint                  = i.ReorderPoint,
        Max                           = i.Max,
        QuantityOnHand                = i.QuantityOnHand,
        AverageCost                   = i.AverageCost,
        QuantityOnOrder               = i.QuantityOnOrder,
        QuantityOnSalesOrder          = i.QuantityOnSalesOrder,
        UserData                      = i.UserData,
        CustomField1                  = i.CustomField1,
        CustomField2                  = i.CustomField2,
        CustomField3                  = i.CustomField3,
        CustomField4                  = i.CustomField4,
        CustomField5                  = i.CustomField5,
        CustomField6                  = i.CustomField6,
        CustomField7                  = i.CustomField7,
        CustomField8                  = i.CustomField8,
        CustomField9                  = i.CustomField9,
        CustomField10                 = i.CustomField10,
        CustomField11                 = i.CustomField11,
        CustomField12                 = i.CustomField12,
        CustomField13                 = i.CustomField13,
        CustomField14                 = i.CustomField14,
        CustomField15                 = i.CustomField15,
        Status                        = i.Status,
        ExternalGUID                  = i.ExternalGUID
    };
}

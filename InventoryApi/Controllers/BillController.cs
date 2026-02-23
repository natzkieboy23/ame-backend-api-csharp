using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Data;
using InventoryApi.DTOs.Bill;
using InventoryApi.DTOs.Common;
using InventoryApi.DTOs.TxnItemLine;
using InventoryApi.Helpers;
using InventoryApi.Models;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/bill")]
public class BillController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public BillController(InventoryDbContext db) => _db = db;

    // ─── Bill endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/bill
    /// Optional filters: ?vendorRef_ListID= &amp;isPaid= &amp;status=
    /// Returns bill headers with their line items.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillResponseDto>>>> GetAll(
        [FromQuery] string? vendorRef_ListID,
        [FromQuery] string? isPaid,
        [FromQuery] string? status)
    {
        IQueryable<Bill> query = _db.Bills.Include(b => b.LineItems);

        if (!string.IsNullOrWhiteSpace(vendorRef_ListID))
            query = query.Where(b => b.VendorRef_ListID == vendorRef_ListID);

        if (!string.IsNullOrWhiteSpace(isPaid))
            query = query.Where(b => b.IsPaid == isPaid);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        var items = await query
            .OrderByDescending(b => b.TxnDate)
            .ToListAsync();

        var dtos = items.Select(MapToResponse).ToList();
        return Ok(ApiResponse<IEnumerable<BillResponseDto>>.Ok(dtos, totalCount: dtos.Count));
    }

    /// <summary>
    /// GET /api/bill/{id}
    /// Returns a single bill with all its line items.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BillResponseDto>>> GetById(string id)
    {
        var entity = await _db.Bills
            .Include(b => b.LineItems)
            .FirstOrDefaultAsync(b => b.TxnID == id);

        if (entity is null)
            return NotFound(ApiResponse<BillResponseDto>
                .Fail($"Bill with TxnID '{id}' was not found."));

        return Ok(ApiResponse<BillResponseDto>.Ok(MapToResponse(entity)));
    }

    /// <summary>
    /// POST /api/bill
    /// Creates a bill header. Optionally accepts LineItems to create in the same request.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BillResponseDto>>> Create(
        [FromBody] BillCreateDto dto)
    {
        var now = DateTime.UtcNow;
        var txnId = Guid.NewGuid().ToString();

        var entity = new Bill
        {
            TxnID                    = txnId,
            TimeCreated              = now,
            TimeModified             = now,
            EditSequence             = EditSequenceHelper.Generate(),
            VendorRef_ListID         = dto.VendorRef_ListID,
            VendorRef_FullName       = dto.VendorRef_FullName,
            APAccountRef_ListID      = dto.APAccountRef_ListID,
            APAccountRef_FullName    = dto.APAccountRef_FullName,
            TxnDate                  = dto.TxnDate ?? now,
            DueDate                  = dto.DueDate,
            CurrencyRef_ListID       = dto.CurrencyRef_ListID,
            CurrencyRef_FullName     = dto.CurrencyRef_FullName,
            RefNumber                = dto.RefNumber,
            TermsRef_ListID          = dto.TermsRef_ListID,
            TermsRef_FullName        = dto.TermsRef_FullName,
            Memo                     = dto.Memo,
            IsTaxIncluded            = dto.IsTaxIncluded,
            SalesTaxCodeRef_ListID   = dto.SalesTaxCodeRef_ListID,
            SalesTaxCodeRef_FullName = dto.SalesTaxCodeRef_FullName,
            IsPaid                   = "false",
            Status                   = dto.Status ?? "ADD",
            VendorAddress_Addr1      = dto.VendorAddress_Addr1,
            VendorAddress_Addr2      = dto.VendorAddress_Addr2,
            VendorAddress_Addr3      = dto.VendorAddress_Addr3,
            VendorAddress_Addr4      = dto.VendorAddress_Addr4,
            VendorAddress_Addr5      = dto.VendorAddress_Addr5,
            VendorAddress_City       = dto.VendorAddress_City,
            VendorAddress_State      = dto.VendorAddress_State,
            VendorAddress_PostalCode = dto.VendorAddress_PostalCode,
            VendorAddress_Country    = dto.VendorAddress_Country,
            VendorAddress_Note       = dto.VendorAddress_Note,
            UserData                 = dto.UserData,
            CustomField1             = dto.CustomField1,
            CustomField2             = dto.CustomField2,
            CustomField3             = dto.CustomField3,
            CustomField4             = dto.CustomField4,
            CustomField5             = dto.CustomField5,
            CustomField6             = dto.CustomField6,
            CustomField7             = dto.CustomField7,
            CustomField8             = dto.CustomField8,
            CustomField9             = dto.CustomField9,
            CustomField10            = dto.CustomField10,
            CustomField11            = dto.CustomField11,
            CustomField12            = dto.CustomField12,
            CustomField13            = dto.CustomField13,
            CustomField14            = dto.CustomField14,
            CustomField15            = dto.CustomField15,
            ExternalGUID             = dto.ExternalGUID,
        };

        if (dto.LineItems is { Count: > 0 })
        {
            var lineEntities = new List<TxnItemLineDetail>();
            for (int i = 0; i < dto.LineItems.Count; i++)
            {
                var line = MapLineToEntity(dto.LineItems[i], txnId, i + 1);
                await EnrichLineCostAsync(line, dto.LineItems[i].Quantity);
                lineEntities.Add(line);
            }
            entity.LineItems = lineEntities;
        }

        _db.Bills.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.TxnID },
            ApiResponse<BillResponseDto>.Ok(MapToResponse(entity), "Created successfully."));
    }

    /// <summary>
    /// PUT /api/bill/{id}
    /// Updates bill header fields only. Use /lines sub-endpoints to manage line items.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<BillResponseDto>>> Update(
        string id, [FromBody] BillUpdateDto dto)
    {
        var entity = await _db.Bills
            .Include(b => b.LineItems)
            .FirstOrDefaultAsync(b => b.TxnID == id);

        if (entity is null)
            return NotFound(ApiResponse<BillResponseDto>
                .Fail($"Bill with TxnID '{id}' was not found."));

        entity.TimeModified             = DateTime.UtcNow;
        entity.EditSequence             = EditSequenceHelper.Increment(entity.EditSequence);
        entity.VendorRef_ListID         = dto.VendorRef_ListID;
        entity.VendorRef_FullName       = dto.VendorRef_FullName;
        entity.APAccountRef_ListID      = dto.APAccountRef_ListID;
        entity.APAccountRef_FullName    = dto.APAccountRef_FullName;
        entity.TxnDate                  = dto.TxnDate;
        entity.DueDate                  = dto.DueDate;
        entity.AmountDue                = dto.AmountDue;
        entity.CurrencyRef_ListID       = dto.CurrencyRef_ListID;
        entity.CurrencyRef_FullName     = dto.CurrencyRef_FullName;
        entity.RefNumber                = dto.RefNumber;
        entity.TermsRef_ListID          = dto.TermsRef_ListID;
        entity.TermsRef_FullName        = dto.TermsRef_FullName;
        entity.Memo                     = dto.Memo;
        entity.IsTaxIncluded            = dto.IsTaxIncluded;
        entity.SalesTaxCodeRef_ListID   = dto.SalesTaxCodeRef_ListID;
        entity.SalesTaxCodeRef_FullName = dto.SalesTaxCodeRef_FullName;
        entity.IsPaid                   = dto.IsPaid;
        entity.OpenAmount               = dto.OpenAmount;
        entity.VendorAddress_Addr1      = dto.VendorAddress_Addr1;
        entity.VendorAddress_Addr2      = dto.VendorAddress_Addr2;
        entity.VendorAddress_Addr3      = dto.VendorAddress_Addr3;
        entity.VendorAddress_Addr4      = dto.VendorAddress_Addr4;
        entity.VendorAddress_Addr5      = dto.VendorAddress_Addr5;
        entity.VendorAddress_City       = dto.VendorAddress_City;
        entity.VendorAddress_State      = dto.VendorAddress_State;
        entity.VendorAddress_PostalCode = dto.VendorAddress_PostalCode;
        entity.VendorAddress_Country    = dto.VendorAddress_Country;
        entity.VendorAddress_Note       = dto.VendorAddress_Note;
        entity.UserData                 = dto.UserData;
        entity.CustomField1             = dto.CustomField1;
        entity.CustomField2             = dto.CustomField2;
        entity.CustomField3             = dto.CustomField3;
        entity.CustomField4             = dto.CustomField4;
        entity.CustomField5             = dto.CustomField5;
        entity.CustomField6             = dto.CustomField6;
        entity.CustomField7             = dto.CustomField7;
        entity.CustomField8             = dto.CustomField8;
        entity.CustomField9             = dto.CustomField9;
        entity.CustomField10            = dto.CustomField10;
        entity.CustomField11            = dto.CustomField11;
        entity.CustomField12            = dto.CustomField12;
        entity.CustomField13            = dto.CustomField13;
        entity.CustomField14            = dto.CustomField14;
        entity.CustomField15            = dto.CustomField15;
        entity.Status                   = dto.Status;
        entity.ExternalGUID             = dto.ExternalGUID;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillResponseDto>.Ok(MapToResponse(entity), "Updated successfully."));
    }

    /// <summary>
    /// DELETE /api/bill/{id}
    /// Soft delete: sets Status = "Deleted".
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var entity = await _db.Bills.FindAsync(id);

        if (entity is null)
            return NotFound(ApiResponse<object>.Fail($"Bill with TxnID '{id}' was not found."));

        entity.Status       = "Deleted";
        entity.TimeModified = DateTime.UtcNow;
        entity.EditSequence = EditSequenceHelper.Increment(entity.EditSequence);

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Bill deleted successfully."));
    }

    // ─── Line item sub-endpoints ──────────────────────────────────────────────

    /// <summary>
    /// GET /api/bill/{txnId}/lines
    /// Returns all line items for a bill.
    /// </summary>
    [HttpGet("{txnId}/lines")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TxnItemLineResponseDto>>>> GetLines(
        string txnId)
    {
        var billExists = await _db.Bills.AnyAsync(b => b.TxnID == txnId);
        if (!billExists)
            return NotFound(ApiResponse<IEnumerable<TxnItemLineResponseDto>>
                .Fail($"Bill with TxnID '{txnId}' was not found."));

        var lines = await _db.TxnItemLineDetails
            .Where(l => l.IDKEY == txnId)
            .Select(l => MapLineToResponse(l))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<TxnItemLineResponseDto>>.Ok(lines, totalCount: lines.Count));
    }

    /// <summary>
    /// POST /api/bill/{txnId}/lines
    /// Adds a new line item to an existing bill.
    /// </summary>
    [HttpPost("{txnId}/lines")]
    public async Task<ActionResult<ApiResponse<TxnItemLineResponseDto>>> CreateLine(
        string txnId, [FromBody] TxnItemLineCreateDto dto)
    {
        var billExists = await _db.Bills.AnyAsync(b => b.TxnID == txnId);
        if (!billExists)
            return NotFound(ApiResponse<TxnItemLineResponseDto>
                .Fail($"Bill with TxnID '{txnId}' was not found."));

        var nextSeq = await _db.TxnItemLineDetails
            .Where(l => l.IDKEY == txnId)
            .CountAsync() + 1;

        var entity = MapLineToEntity(dto, txnId, nextSeq);
        await EnrichLineCostAsync(entity, dto.Quantity);
        _db.TxnItemLineDetails.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLines), new { txnId },
            ApiResponse<TxnItemLineResponseDto>.Ok(MapLineToResponse(entity), "Line item created."));
    }

    /// <summary>
    /// PUT /api/bill/{txnId}/lines/{lineId}
    /// Updates a specific line item.
    /// </summary>
    [HttpPut("{txnId}/lines/{lineId}")]
    public async Task<ActionResult<ApiResponse<TxnItemLineResponseDto>>> UpdateLine(
        string txnId, string lineId, [FromBody] TxnItemLineUpdateDto dto)
    {
        var entity = await _db.TxnItemLineDetails
            .FirstOrDefaultAsync(l => l.TxnLineID == lineId && l.IDKEY == txnId);

        if (entity is null)
            return NotFound(ApiResponse<TxnItemLineResponseDto>
                .Fail($"Line item '{lineId}' not found on bill '{txnId}'."));

        entity.ItemRef_ListID             = dto.ItemRef_ListID;
        entity.ItemRef_FullName           = dto.ItemRef_FullName;
        entity.Description               = dto.Description;
        entity.Quantity                  = dto.Quantity;
        entity.UnitOfMeasure             = dto.UnitOfMeasure;
        entity.Cost                      = dto.Cost;
        entity.Amount                    = dto.Amount;
        entity.InventorySiteRef_ListID   = dto.InventorySiteRef_ListID;
        entity.InventorySiteRef_FullName = dto.InventorySiteRef_FullName;
        entity.SerialNumber              = dto.SerialNumber;
        entity.LotNumber                 = dto.LotNumber;
        entity.SalesTaxCodeRef_ListID    = dto.SalesTaxCodeRef_ListID;
        entity.SalesTaxCodeRef_FullName  = dto.SalesTaxCodeRef_FullName;
        entity.ClassRef_ListID           = dto.ClassRef_ListID;
        entity.ClassRef_FullName         = dto.ClassRef_FullName;
        entity.CustomField1              = dto.CustomField1;
        entity.CustomField2              = dto.CustomField2;
        entity.CustomField3              = dto.CustomField3;
        entity.CustomField4              = dto.CustomField4;
        entity.CustomField5              = dto.CustomField5;
        entity.CustomField6              = dto.CustomField6;
        entity.CustomField7              = dto.CustomField7;
        entity.CustomField8              = dto.CustomField8;
        entity.CustomField9              = dto.CustomField9;
        entity.CustomField10             = dto.CustomField10;
        entity.CustomField11             = dto.CustomField11;
        entity.CustomField12             = dto.CustomField12;
        entity.CustomField13             = dto.CustomField13;
        entity.CustomField14             = dto.CustomField14;
        entity.CustomField15             = dto.CustomField15;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<TxnItemLineResponseDto>.Ok(MapLineToResponse(entity), "Line item updated."));
    }

    /// <summary>
    /// DELETE /api/bill/{txnId}/lines/{lineId}
    /// Hard deletes a line item.
    /// </summary>
    [HttpDelete("{txnId}/lines/{lineId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLine(
        string txnId, string lineId)
    {
        var entity = await _db.TxnItemLineDetails
            .FirstOrDefaultAsync(l => l.TxnLineID == lineId && l.IDKEY == txnId);

        if (entity is null)
            return NotFound(ApiResponse<object>
                .Fail($"Line item '{lineId}' not found on bill '{txnId}'."));

        _db.TxnItemLineDetails.Remove(entity);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Line item deleted."));
    }

    // ─── Mappers ──────────────────────────────────────────────────────────────

    private static BillResponseDto MapToResponse(Bill b) => new()
    {
        TxnID                    = b.TxnID,
        TimeCreated              = b.TimeCreated,
        TimeModified             = b.TimeModified,
        EditSequence             = b.EditSequence,
        TxnNumber                = b.TxnNumber,
        VendorRef_ListID         = b.VendorRef_ListID,
        VendorRef_FullName       = b.VendorRef_FullName,
        APAccountRef_ListID      = b.APAccountRef_ListID,
        APAccountRef_FullName    = b.APAccountRef_FullName,
        TxnDate                  = b.TxnDate,
        DueDate                  = b.DueDate,
        AmountDue                = b.AmountDue,
        CurrencyRef_ListID       = b.CurrencyRef_ListID,
        CurrencyRef_FullName     = b.CurrencyRef_FullName,
        ExchangeRate             = b.ExchangeRate,
        AmountDueInHomeCurrency  = b.AmountDueInHomeCurrency,
        RefNumber                = b.RefNumber,
        TermsRef_ListID          = b.TermsRef_ListID,
        TermsRef_FullName        = b.TermsRef_FullName,
        Memo                     = b.Memo,
        IsTaxIncluded            = b.IsTaxIncluded,
        SalesTaxCodeRef_ListID   = b.SalesTaxCodeRef_ListID,
        SalesTaxCodeRef_FullName = b.SalesTaxCodeRef_FullName,
        IsPaid                   = b.IsPaid,
        VendorAddress_Addr1      = b.VendorAddress_Addr1,
        VendorAddress_Addr2      = b.VendorAddress_Addr2,
        VendorAddress_Addr3      = b.VendorAddress_Addr3,
        VendorAddress_Addr4      = b.VendorAddress_Addr4,
        VendorAddress_Addr5      = b.VendorAddress_Addr5,
        VendorAddress_City       = b.VendorAddress_City,
        VendorAddress_State      = b.VendorAddress_State,
        VendorAddress_PostalCode = b.VendorAddress_PostalCode,
        VendorAddress_Country    = b.VendorAddress_Country,
        VendorAddress_Note       = b.VendorAddress_Note,
        OpenAmount               = b.OpenAmount,
        UserData                 = b.UserData,
        CustomField1             = b.CustomField1,
        CustomField2             = b.CustomField2,
        CustomField3             = b.CustomField3,
        CustomField4             = b.CustomField4,
        CustomField5             = b.CustomField5,
        CustomField6             = b.CustomField6,
        CustomField7             = b.CustomField7,
        CustomField8             = b.CustomField8,
        CustomField9             = b.CustomField9,
        CustomField10            = b.CustomField10,
        CustomField11            = b.CustomField11,
        CustomField12            = b.CustomField12,
        CustomField13            = b.CustomField13,
        CustomField14            = b.CustomField14,
        CustomField15            = b.CustomField15,
        Status                   = b.Status,
        ExternalGUID             = b.ExternalGUID,
        LineItems                = b.LineItems.Select(MapLineToResponse).ToList(),
    };

    private static TxnItemLineDetail MapLineToEntity(TxnItemLineCreateDto dto, string txnId, int seqNum) => new()
    {
        TxnLineID                = Guid.NewGuid().ToString(),
        IDKEY                    = txnId,
        SeqNum                   = seqNum,
        ItemRef_ListID           = dto.ItemRef_ListID,
        ItemRef_FullName         = dto.ItemRef_FullName,
        Description              = dto.Description,
        Quantity                 = dto.Quantity,
        UnitOfMeasure            = dto.UnitOfMeasure,
        Cost                     = dto.Cost,
        Amount                   = dto.Amount,
        InventorySiteRef_ListID  = dto.InventorySiteRef_ListID,
        InventorySiteRef_FullName= dto.InventorySiteRef_FullName,
        SerialNumber             = dto.SerialNumber,
        LotNumber                = dto.LotNumber,
        SalesTaxCodeRef_ListID   = dto.SalesTaxCodeRef_ListID,
        SalesTaxCodeRef_FullName = dto.SalesTaxCodeRef_FullName,
        ClassRef_ListID          = dto.ClassRef_ListID,
        ClassRef_FullName        = dto.ClassRef_FullName,
        CustomField1             = dto.CustomField1,
        CustomField2             = dto.CustomField2,
        CustomField3             = dto.CustomField3,
        CustomField4             = dto.CustomField4,
        CustomField5             = dto.CustomField5,
        CustomField6             = dto.CustomField6,
        CustomField7             = dto.CustomField7,
        CustomField8             = dto.CustomField8,
        CustomField9             = dto.CustomField9,
        CustomField10            = dto.CustomField10,
        CustomField11            = dto.CustomField11,
        CustomField12            = dto.CustomField12,
        CustomField13            = dto.CustomField13,
        CustomField14            = dto.CustomField14,
        CustomField15            = dto.CustomField15,
    };

    private static TxnItemLineResponseDto MapLineToResponse(TxnItemLineDetail l) => new()
    {
        TxnLineID                = l.TxnLineID,
        IDKEY                    = l.IDKEY,
        SeqNum                   = l.SeqNum,
        ItemRef_ListID           = l.ItemRef_ListID,
        ItemRef_FullName         = l.ItemRef_FullName,
        Description              = l.Description,
        Quantity                 = l.Quantity,
        UnitOfMeasure            = l.UnitOfMeasure,
        Cost                     = l.Cost,
        Amount                   = l.Amount,
        InventorySiteRef_ListID  = l.InventorySiteRef_ListID,
        InventorySiteRef_FullName= l.InventorySiteRef_FullName,
        SerialNumber             = l.SerialNumber,
        LotNumber                = l.LotNumber,
        SalesTaxCodeRef_ListID   = l.SalesTaxCodeRef_ListID,
        SalesTaxCodeRef_FullName = l.SalesTaxCodeRef_FullName,
        ClassRef_ListID          = l.ClassRef_ListID,
        ClassRef_FullName        = l.ClassRef_FullName,
        CustomField1             = l.CustomField1,
        CustomField2             = l.CustomField2,
        CustomField3             = l.CustomField3,
        CustomField4             = l.CustomField4,
        CustomField5             = l.CustomField5,
        CustomField6             = l.CustomField6,
        CustomField7             = l.CustomField7,
        CustomField8             = l.CustomField8,
        CustomField9             = l.CustomField9,
        CustomField10            = l.CustomField10,
        CustomField11            = l.CustomField11,
        CustomField12            = l.CustomField12,
        CustomField13            = l.CustomField13,
        CustomField14            = l.CustomField14,
        CustomField15            = l.CustomField15,
    };

    // Looks up iteminventory.PurchaseCost by ItemRef_ListID and sets
    // Cost = PurchaseCost, Amount = Quantity × PurchaseCost on the line entity.
    private async Task EnrichLineCostAsync(TxnItemLineDetail line, decimal? quantity)
    {
        if (string.IsNullOrWhiteSpace(line.ItemRef_ListID)) return;

        var purchaseCost = await _db.ItemInventories
            .Where(ii => ii.ListID == line.ItemRef_ListID)
            .Select(ii => ii.PurchaseCost)
            .FirstOrDefaultAsync();

        if (purchaseCost.HasValue)
        {
            line.Cost   = purchaseCost.Value.ToString("0.######");
            line.Amount = quantity.HasValue ? quantity.Value * purchaseCost.Value : null;
        }
    }
}

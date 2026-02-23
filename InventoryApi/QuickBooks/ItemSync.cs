using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Pulls all ItemInventory records from QuickBooks and upserts them into
/// the MySQL `iteminventory` table, keyed on ListID.
/// </summary>
public class ItemSync(string mysqlConn, QbSession session)
{
    // ── QBXML request ────────────────────────────────────────────────────────

    private string BuildRequest() => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <?qbxml version="{session.QbXmlVersion}"?>
        <QBXML>
          <QBXMLMsgsRq onError="continueOnError">
            <ItemInventoryQueryRq requestID="1">
              <ActiveStatus>All</ActiveStatus>
              <OwnerID>0</OwnerID>
            </ItemInventoryQueryRq>
          </QBXMLMsgsRq>
        </QBXML>
        """;

    // ── Public entry point ───────────────────────────────────────────────────

    public async Task<SyncResult> RunAsync()
    {
        Console.WriteLine("  Sending ItemInventoryQueryRq to QuickBooks...");
        var responseXml = session.DoRequests(BuildRequest());

        var items = ParseResponse(responseXml);
        Console.WriteLine($"  Received {items.Count} item(s) from QB.");

        if (items.Count == 0) return new SyncResult();

        return await UpsertAsync(items);
    }

    // ── XML parsing ──────────────────────────────────────────────────────────

    private static List<ItemRow> ParseResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var rs  = doc.Descendants("ItemInventoryQueryRs").FirstOrDefault()
                  ?? throw new InvalidOperationException("ItemInventoryQueryRs not found in QB response.");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status != "0")
        {
            var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
            throw new InvalidOperationException($"QB ItemInventoryQueryRs error {status}: {msg}");
        }

        return rs.Elements("ItemInventoryRet").Select(e => new ItemRow
        {
            ListID       = Val(e, "ListID") ?? string.Empty,
            TimeCreated  = ParseDt(Val(e, "TimeCreated")),
            TimeModified = ParseDt(Val(e, "TimeModified")),
            EditSequence = Val(e, "EditSequence"),
            Name         = Val(e, "Name"),
            FullName     = Val(e, "FullName"),
            BarCodeValue = Val(e, "BarCodeValue"),
            IsActive     = Val(e, "IsActive"),
            Sublevel     = ParseInt(Val(e, "Sublevel")),
            ManufacturerPartNumber = Val(e, "ManufacturerPartNumber"),

            ParentRef_ListID   = Val(e, "ParentRef/ListID"),
            ParentRef_FullName = Val(e, "ParentRef/FullName"),
            ClassRef_ListID    = Val(e, "ClassRef/ListID"),
            ClassRef_FullName  = Val(e, "ClassRef/FullName"),

            UnitOfMeasureSetRef_ListID   = Val(e, "UnitOfMeasureSetRef/ListID"),
            UnitOfMeasureSetRef_FullName = Val(e, "UnitOfMeasureSetRef/FullName"),

            SalesTaxCodeRef_ListID   = Val(e, "SalesTaxCodeRef/ListID"),
            SalesTaxCodeRef_FullName = Val(e, "SalesTaxCodeRef/FullName"),
            IsTaxIncluded            = Val(e, "IsTaxIncluded"),

            SalesDesc = Val(e, "SalesDesc"),
            SalesPrice = ParseDec(Val(e, "SalesPrice")),
            IncomeAccountRef_ListID   = Val(e, "IncomeAccountRef/ListID"),
            IncomeAccountRef_FullName = Val(e, "IncomeAccountRef/FullName"),

            PurchaseDesc = Val(e, "PurchaseDesc"),
            PurchaseCost = ParseDec(Val(e, "PurchaseCost")),
            COGSAccountRef_ListID   = Val(e, "COGSAccountRef/ListID"),
            COGSAccountRef_FullName = Val(e, "COGSAccountRef/FullName"),
            PrefVendorRef_ListID    = Val(e, "PrefVendorRef/ListID"),
            PrefVendorRef_FullName  = Val(e, "PrefVendorRef/FullName"),
            AssetAccountRef_ListID  = Val(e, "AssetAccountRef/ListID"),
            AssetAccountRef_FullName = Val(e, "AssetAccountRef/FullName"),

            ReorderPoint      = ParseInt(Val(e, "ReorderPoint")),
            Max               = ParseInt(Val(e, "Max")),
            QuantityOnHand    = ParseDec(Val(e, "QuantityOnHand")),
            AverageCost       = ParseDec(Val(e, "AverageCost")),
            QuantityOnOrder   = ParseDec(Val(e, "QuantityOnOrder")),
            QuantityOnSalesOrder = ParseDec(Val(e, "QuantityOnSalesOrder")),
        }).ToList();
    }

    // ── MySQL upsert ─────────────────────────────────────────────────────────

    private async Task<SyncResult> UpsertAsync(List<ItemRow> rows)
    {
        const string sql = """
            INSERT INTO iteminventory (
                ListID, TimeCreated, TimeModified, EditSequence,
                Name, FullName, BarCodeValue, IsActive, Sublevel,
                ManufacturerPartNumber,
                ParentRef_ListID, ParentRef_FullName,
                ClassRef_ListID, ClassRef_FullName,
                UnitOfMeasureSetRef_ListID, UnitOfMeasureSetRef_FullName,
                SalesTaxCodeRef_ListID, SalesTaxCodeRef_FullName, IsTaxIncluded,
                SalesDesc, SalesPrice,
                IncomeAccountRef_ListID, IncomeAccountRef_FullName,
                PurchaseDesc, PurchaseCost,
                COGSAccountRef_ListID, COGSAccountRef_FullName,
                PrefVendorRef_ListID, PrefVendorRef_FullName,
                AssetAccountRef_ListID, AssetAccountRef_FullName,
                ReorderPoint, Max,
                QuantityOnHand, AverageCost, QuantityOnOrder, QuantityOnSalesOrder
            ) VALUES (
                @ListID, @TimeCreated, @TimeModified, @EditSequence,
                @Name, @FullName, @BarCodeValue, @IsActive, @Sublevel,
                @ManufacturerPartNumber,
                @ParentRef_ListID, @ParentRef_FullName,
                @ClassRef_ListID, @ClassRef_FullName,
                @UnitOfMeasureSetRef_ListID, @UnitOfMeasureSetRef_FullName,
                @SalesTaxCodeRef_ListID, @SalesTaxCodeRef_FullName, @IsTaxIncluded,
                @SalesDesc, @SalesPrice,
                @IncomeAccountRef_ListID, @IncomeAccountRef_FullName,
                @PurchaseDesc, @PurchaseCost,
                @COGSAccountRef_ListID, @COGSAccountRef_FullName,
                @PrefVendorRef_ListID, @PrefVendorRef_FullName,
                @AssetAccountRef_ListID, @AssetAccountRef_FullName,
                @ReorderPoint, @Max,
                @QuantityOnHand, @AverageCost, @QuantityOnOrder, @QuantityOnSalesOrder
            )
            ON DUPLICATE KEY UPDATE
                TimeModified                 = VALUES(TimeModified),
                EditSequence                 = VALUES(EditSequence),
                Name                         = VALUES(Name),
                FullName                     = VALUES(FullName),
                BarCodeValue                 = VALUES(BarCodeValue),
                IsActive                     = VALUES(IsActive),
                Sublevel                     = VALUES(Sublevel),
                ManufacturerPartNumber       = VALUES(ManufacturerPartNumber),
                ParentRef_ListID             = VALUES(ParentRef_ListID),
                ParentRef_FullName           = VALUES(ParentRef_FullName),
                ClassRef_ListID              = VALUES(ClassRef_ListID),
                ClassRef_FullName            = VALUES(ClassRef_FullName),
                UnitOfMeasureSetRef_ListID   = VALUES(UnitOfMeasureSetRef_ListID),
                UnitOfMeasureSetRef_FullName = VALUES(UnitOfMeasureSetRef_FullName),
                SalesTaxCodeRef_ListID       = VALUES(SalesTaxCodeRef_ListID),
                SalesTaxCodeRef_FullName     = VALUES(SalesTaxCodeRef_FullName),
                IsTaxIncluded                = VALUES(IsTaxIncluded),
                SalesDesc                    = VALUES(SalesDesc),
                SalesPrice                   = VALUES(SalesPrice),
                IncomeAccountRef_ListID      = VALUES(IncomeAccountRef_ListID),
                IncomeAccountRef_FullName    = VALUES(IncomeAccountRef_FullName),
                PurchaseDesc                 = VALUES(PurchaseDesc),
                PurchaseCost                 = VALUES(PurchaseCost),
                COGSAccountRef_ListID        = VALUES(COGSAccountRef_ListID),
                COGSAccountRef_FullName      = VALUES(COGSAccountRef_FullName),
                PrefVendorRef_ListID         = VALUES(PrefVendorRef_ListID),
                PrefVendorRef_FullName       = VALUES(PrefVendorRef_FullName),
                AssetAccountRef_ListID       = VALUES(AssetAccountRef_ListID),
                AssetAccountRef_FullName     = VALUES(AssetAccountRef_FullName),
                ReorderPoint                 = VALUES(ReorderPoint),
                Max                          = VALUES(Max),
                QuantityOnHand               = VALUES(QuantityOnHand),
                AverageCost                  = VALUES(AverageCost),
                QuantityOnOrder              = VALUES(QuantityOnOrder),
                QuantityOnSalesOrder         = VALUES(QuantityOnSalesOrder)
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        await conn.OpenAsync();

        int inserted = 0, updated = 0;
        foreach (var row in rows)
        {
            int aff = await conn.ExecuteAsync(sql, row);
            if (aff == 1) inserted++; else updated++;
        }
        return new SyncResult { Total = rows.Count, Inserted = inserted, Updated = updated };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? Val(XElement el, string path)
    {
        var parts = path.Split('/');
        XElement? cur = el;
        foreach (var p in parts) { cur = cur?.Element(p); if (cur is null) return null; }
        return string.IsNullOrWhiteSpace(cur?.Value) ? null : cur.Value.Trim();
    }

    private static DateTime? ParseDt(string? s)  => DateTime.TryParse(s, out var d) ? d : null;
    private static decimal?  ParseDec(string? s) => decimal.TryParse(s, out var d) ? d : null;
    private static int?      ParseInt(string? s)  => int.TryParse(s, out var i) ? i : null;
}

// ── Internal row model ────────────────────────────────────────────────────────

internal class ItemRow
{
    public string  ListID       { get; set; } = string.Empty;
    public DateTime? TimeCreated  { get; set; }
    public DateTime? TimeModified { get; set; }
    public string? EditSequence  { get; set; }
    public string? Name          { get; set; }
    public string? FullName      { get; set; }
    public string? BarCodeValue  { get; set; }
    public string? IsActive      { get; set; }
    public int?    Sublevel      { get; set; }
    public string? ManufacturerPartNumber       { get; set; }
    public string? ParentRef_ListID             { get; set; }
    public string? ParentRef_FullName           { get; set; }
    public string? ClassRef_ListID              { get; set; }
    public string? ClassRef_FullName            { get; set; }
    public string? UnitOfMeasureSetRef_ListID   { get; set; }
    public string? UnitOfMeasureSetRef_FullName { get; set; }
    public string? SalesTaxCodeRef_ListID       { get; set; }
    public string? SalesTaxCodeRef_FullName     { get; set; }
    public string? IsTaxIncluded                { get; set; }
    public string? SalesDesc                    { get; set; }
    public decimal? SalesPrice                  { get; set; }
    public string? IncomeAccountRef_ListID      { get; set; }
    public string? IncomeAccountRef_FullName    { get; set; }
    public string? PurchaseDesc                 { get; set; }
    public decimal? PurchaseCost                { get; set; }
    public string? COGSAccountRef_ListID        { get; set; }
    public string? COGSAccountRef_FullName      { get; set; }
    public string? PrefVendorRef_ListID         { get; set; }
    public string? PrefVendorRef_FullName       { get; set; }
    public string? AssetAccountRef_ListID       { get; set; }
    public string? AssetAccountRef_FullName     { get; set; }
    public int?    ReorderPoint                 { get; set; }
    public int?    Max                          { get; set; }
    public decimal? QuantityOnHand              { get; set; }
    public decimal? AverageCost                 { get; set; }
    public decimal? QuantityOnOrder             { get; set; }
    public decimal? QuantityOnSalesOrder        { get; set; }
}

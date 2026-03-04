using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Pulls all ItemSites records from QuickBooks and upserts them into
/// the MySQL `itemsites` table, keyed on ListID.
/// Captures per-site quantities for every inventory and assembly item.
/// </summary>
public class ItemSiteSync(string mysqlConn, QbSession session)
{
    // ── QBXML request ────────────────────────────────────────────────────────

    private string BuildRequest() => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <?qbxml version="{session.QbXmlVersion}"?>
        <QBXML>
          <QBXMLMsgsRq onError="continueOnError">
            <ItemSitesQueryRq requestID="1">
              <ActiveStatus>All</ActiveStatus>
            </ItemSitesQueryRq>
          </QBXMLMsgsRq>
        </QBXML>
        """;

    // ── Public entry point ───────────────────────────────────────────────────

    public async Task<SyncResult> RunAsync()
    {
        Console.WriteLine("  Sending ItemSitesQueryRq to QuickBooks...");
        var responseXml = session.DoRequests(BuildRequest());

        var rows = ParseResponse(responseXml);
        Console.WriteLine($"  Received {rows.Count} item site record(s) from QB.");

        if (rows.Count == 0) return new SyncResult();

        return await UpsertAsync(rows);
    }

    // ── XML parsing ──────────────────────────────────────────────────────────

    private static List<ItemSiteRow> ParseResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var rs  = doc.Descendants("ItemSitesQueryRs").FirstOrDefault()
                  ?? throw new InvalidOperationException("ItemSitesQueryRs not found in QB response.");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status != "0")
        {
            var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
            throw new InvalidOperationException($"QB ItemSitesQueryRs error {status}: {msg}");
        }

        return rs.Elements("ItemSitesRet").Select(e => new ItemSiteRow
        {
            ListID       = Val(e, "ListID") ?? string.Empty,
            TimeCreated  = ParseDt(Val(e, "TimeCreated")),
            TimeModified = ParseDt(Val(e, "TimeModified")),
            EditSequence = Val(e, "EditSequence"),

            ItemInventoryAssemblyRef_ListID   = Val(e, "ItemInventoryAssemblyRef/ListID"),
            ItemInventoryAssemblyRef_FullName = Val(e, "ItemInventoryAssemblyRef/FullName"),

            ItemInventoryRef_ListID   = Val(e, "ItemInventoryRef/ListID"),
            ItemInventoryRef_FullName = Val(e, "ItemInventoryRef/FullName"),

            InventorySiteRef_ListID   = Val(e, "InventorySiteRef/ListID"),
            InventorySiteRef_FullName = Val(e, "InventorySiteRef/FullName"),

            InventorySiteLocationRef_ListID   = Val(e, "InventorySiteLocationRef/ListID"),
            InventorySiteLocationRef_FullName = Val(e, "InventorySiteLocationRef/FullName"),

            ReorderLevel                          = Val(e, "ReorderLevel"),
            QuantityOnHand                        = ParseDec(Val(e, "QuantityOnHand")),
            QuantityOnPurchaseOrders              = ParseDec(Val(e, "QuantityOnPurchaseOrders")),
            QuantityOnSalesOrders                 = ParseDec(Val(e, "QuantityOnSalesOrders")),
            QuantityToBeBuiltByPendingBuildTxns   = ParseDec(Val(e, "QuantityToBeBuiltByPendingBuildTxns")),
            QuantityRequiredByPendingBuildTxns    = ParseDec(Val(e, "QuantityRequiredByPendingBuildTxns")),
            QuantityOnPendingTransfers            = ParseDec(Val(e, "QuantityOnPendingTransfers")),
            AssemblyBuildPoint                    = ParseDec(Val(e, "AssemblyBuildPoint")),

            UserData = Val(e, "UserData"),
            Status   = Val(e, "Status"),
        }).ToList();
    }

    // ── MySQL upsert ─────────────────────────────────────────────────────────

    private async Task<SyncResult> UpsertAsync(List<ItemSiteRow> rows)
    {
        const string sql = """
            INSERT INTO itemsites (
                ListID, TimeCreated, TimeModified, EditSequence,
                ItemInventoryAssemblyRef_ListID, ItemInventoryAssemblyRef_FullName,
                ItemInventoryRef_ListID, ItemInventoryRef_FullName,
                InventorySiteRef_ListID, InventorySiteRef_FullName,
                InventorySiteLocationRef_ListID, InventorySiteLocationRef_FullName,
                ReorderLevel,
                QuantityOnHand, QuantityOnPurchaseOrders, QuantityOnSalesOrders,
                QuantityToBeBuiltByPendingBuildTxns, QuantityRequiredByPendingBuildTxns,
                QuantityOnPendingTransfers, AssemblyBuildPoint,
                UserData, Status
            ) VALUES (
                @ListID, @TimeCreated, @TimeModified, @EditSequence,
                @ItemInventoryAssemblyRef_ListID, @ItemInventoryAssemblyRef_FullName,
                @ItemInventoryRef_ListID, @ItemInventoryRef_FullName,
                @InventorySiteRef_ListID, @InventorySiteRef_FullName,
                @InventorySiteLocationRef_ListID, @InventorySiteLocationRef_FullName,
                @ReorderLevel,
                @QuantityOnHand, @QuantityOnPurchaseOrders, @QuantityOnSalesOrders,
                @QuantityToBeBuiltByPendingBuildTxns, @QuantityRequiredByPendingBuildTxns,
                @QuantityOnPendingTransfers, @AssemblyBuildPoint,
                @UserData, @Status
            )
            ON DUPLICATE KEY UPDATE
                TimeModified                          = VALUES(TimeModified),
                EditSequence                          = VALUES(EditSequence),
                ItemInventoryAssemblyRef_ListID       = VALUES(ItemInventoryAssemblyRef_ListID),
                ItemInventoryAssemblyRef_FullName     = VALUES(ItemInventoryAssemblyRef_FullName),
                ItemInventoryRef_ListID               = VALUES(ItemInventoryRef_ListID),
                ItemInventoryRef_FullName             = VALUES(ItemInventoryRef_FullName),
                InventorySiteRef_ListID               = VALUES(InventorySiteRef_ListID),
                InventorySiteRef_FullName             = VALUES(InventorySiteRef_FullName),
                InventorySiteLocationRef_ListID       = VALUES(InventorySiteLocationRef_ListID),
                InventorySiteLocationRef_FullName     = VALUES(InventorySiteLocationRef_FullName),
                ReorderLevel                          = VALUES(ReorderLevel),
                QuantityOnHand                        = VALUES(QuantityOnHand),
                QuantityOnPurchaseOrders              = VALUES(QuantityOnPurchaseOrders),
                QuantityOnSalesOrders                 = VALUES(QuantityOnSalesOrders),
                QuantityToBeBuiltByPendingBuildTxns   = VALUES(QuantityToBeBuiltByPendingBuildTxns),
                QuantityRequiredByPendingBuildTxns    = VALUES(QuantityRequiredByPendingBuildTxns),
                QuantityOnPendingTransfers            = VALUES(QuantityOnPendingTransfers),
                AssemblyBuildPoint                    = VALUES(AssemblyBuildPoint),
                UserData                              = VALUES(UserData),
                Status                               = VALUES(Status)
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
}

// ── Internal row model ────────────────────────────────────────────────────────

internal class ItemSiteRow
{
    public string    ListID       { get; set; } = string.Empty;
    public DateTime? TimeCreated  { get; set; }
    public DateTime? TimeModified { get; set; }
    public string?   EditSequence { get; set; }

    public string? ItemInventoryAssemblyRef_ListID   { get; set; }
    public string? ItemInventoryAssemblyRef_FullName { get; set; }

    public string? ItemInventoryRef_ListID   { get; set; }
    public string? ItemInventoryRef_FullName { get; set; }

    public string? InventorySiteRef_ListID   { get; set; }
    public string? InventorySiteRef_FullName { get; set; }

    public string? InventorySiteLocationRef_ListID   { get; set; }
    public string? InventorySiteLocationRef_FullName { get; set; }

    public string?   ReorderLevel                        { get; set; }
    public decimal?  QuantityOnHand                      { get; set; }
    public decimal?  QuantityOnPurchaseOrders            { get; set; }
    public decimal?  QuantityOnSalesOrders               { get; set; }
    public decimal?  QuantityToBeBuiltByPendingBuildTxns { get; set; }
    public decimal?  QuantityRequiredByPendingBuildTxns  { get; set; }
    public decimal?  QuantityOnPendingTransfers          { get; set; }
    public decimal?  AssemblyBuildPoint                  { get; set; }

    public string? UserData { get; set; }
    public string? Status   { get; set; }
}

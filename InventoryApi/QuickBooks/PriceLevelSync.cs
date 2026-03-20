using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Pulls all PriceLevel records from QuickBooks and upserts them into
/// the MySQL `pricelevel` (parent) and `pricelevelperitemdetail` (child) tables,
/// keyed on ListID / IDKEY.
/// </summary>
public class PriceLevelSync(string mysqlConn, QbSession session)
{
    // ── QBXML request ────────────────────────────────────────────────────────

    private string BuildRequest() => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <?qbxml version="{session.QbXmlVersion}"?>
        <QBXML>
          <QBXMLMsgsRq onError="continueOnError">
            <PriceLevelQueryRq requestID="1">
              <ActiveStatus>All</ActiveStatus>
            </PriceLevelQueryRq>
          </QBXMLMsgsRq>
        </QBXML>
        """;

    // ── Public entry point ───────────────────────────────────────────────────

    public async Task<SyncResult> RunAsync()
    {
        Console.WriteLine("  Sending PriceLevelQueryRq to QuickBooks...");
        var responseXml = session.DoRequests(BuildRequest());

        var (parents, children) = ParseResponse(responseXml);
        Console.WriteLine($"  Received {parents.Count} price level(s) from QB.");

        if (parents.Count == 0) return new SyncResult();

        return await UpsertAsync(parents, children);
    }

    // ── XML parsing ──────────────────────────────────────────────────────────

    private static (List<PriceLevelRow> parents, List<PriceLevelPerItemRow> children)
        ParseResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var rs  = doc.Descendants("PriceLevelQueryRs").FirstOrDefault()
                  ?? throw new InvalidOperationException("PriceLevelQueryRs not found in QB response.");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status != "0")
        {
            var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
            throw new InvalidOperationException($"QB PriceLevelQueryRs error {status}: {msg}");
        }

        var parents  = new List<PriceLevelRow>();
        var children = new List<PriceLevelPerItemRow>();

        int seq = 0;
        foreach (var e in rs.Elements("PriceLevelRet"))
        {
            var listId = Val(e, "ListID") ?? string.Empty;

            parents.Add(new PriceLevelRow
            {
                ListID                   = listId,
                TimeCreated              = ParseDt(Val(e, "TimeCreated")),
                TimeModified             = ParseDt(Val(e, "TimeModified")),
                EditSequence             = Val(e, "EditSequence"),
                Name                     = Val(e, "Name"),
                IsActive                 = Val(e, "IsActive"),
                PriceLevelType           = Val(e, "PriceLevelType"),
                PriceLevelFixedPercentage = Val(e, "PriceLevelFixedPercentage"),
                CurrencyRef_ListID       = Val(e, "CurrencyRef/ListID"),
                CurrencyRef_FullName     = Val(e, "CurrencyRef/FullName"),
                UserData                 = Val(e, "UserData"),
            });

            seq = 0;
            foreach (var d in e.Elements("PriceLevelPerItemRet"))
            {
                children.Add(new PriceLevelPerItemRow
                {
                    IDKEY                = listId,
                    SeqNum               = seq++,
                    ItemRef_ListID       = Val(d, "ItemRef/ListID"),
                    ItemRef_FullName     = Val(d, "ItemRef/FullName"),
                    CustomPrice          = ParseDec(Val(d, "CustomPrice")),
                    CustomPricePercent   = Val(d, "CustomPricePercent"),
                    AdjustPercentage     = Val(d, "AdjustPercentage"),
                    AdjustRelativeTo     = Val(d, "AdjustRelativeTo"),
                });
            }
        }

        return (parents, children);
    }

    // ── MySQL upsert ─────────────────────────────────────────────────────────

    private async Task<SyncResult> UpsertAsync(
        List<PriceLevelRow> parents,
        List<PriceLevelPerItemRow> children)
    {
        const string parentSql = """
            INSERT INTO pricelevel (
                ListID, TimeCreated, TimeModified, EditSequence,
                Name, IsActive, PriceLevelType, PriceLevelFixedPercentage,
                CurrencyRef_ListID, CurrencyRef_FullName, UserData
            ) VALUES (
                @ListID, @TimeCreated, @TimeModified, @EditSequence,
                @Name, @IsActive, @PriceLevelType, @PriceLevelFixedPercentage,
                @CurrencyRef_ListID, @CurrencyRef_FullName, @UserData
            )
            ON DUPLICATE KEY UPDATE
                TimeModified              = VALUES(TimeModified),
                EditSequence              = VALUES(EditSequence),
                Name                      = VALUES(Name),
                IsActive                  = VALUES(IsActive),
                PriceLevelType            = VALUES(PriceLevelType),
                PriceLevelFixedPercentage = VALUES(PriceLevelFixedPercentage),
                CurrencyRef_ListID        = VALUES(CurrencyRef_ListID),
                CurrencyRef_FullName      = VALUES(CurrencyRef_FullName),
                UserData                  = VALUES(UserData)
            """;

        const string deleteChildSql = """
            DELETE FROM pricelevelperitemdetail WHERE IDKEY = @IDKEY
            """;

        const string childSql = """
            INSERT INTO pricelevelperitemdetail (
                IDKEY, SeqNum,
                ItemRef_ListID, ItemRef_FullName,
                CustomPrice, CustomPricePercent,
                AdjustPercentage, AdjustRelativeTo
            ) VALUES (
                @IDKEY, @SeqNum,
                @ItemRef_ListID, @ItemRef_FullName,
                @CustomPrice, @CustomPricePercent,
                @AdjustPercentage, @AdjustRelativeTo
            )
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        await conn.OpenAsync();

        int inserted = 0, updated = 0;

        foreach (var parent in parents)
        {
            int aff = await conn.ExecuteAsync(parentSql, parent);
            if (aff == 1) inserted++; else updated++;

            // Replace child rows for this price level
            await conn.ExecuteAsync(deleteChildSql, new { parent.ListID });

            var rows = children.Where(c => c.IDKEY == parent.ListID).ToList();
            if (rows.Count > 0)
                await conn.ExecuteAsync(childSql, rows);
        }

        return new SyncResult { Total = parents.Count, Inserted = inserted, Updated = updated };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? Val(XElement el, string path)
    {
        var parts = path.Split('/');
        XElement? cur = el;
        foreach (var p in parts) { cur = cur?.Element(p); if (cur is null) return null; }
        return string.IsNullOrWhiteSpace(cur?.Value) ? null : cur.Value.Trim();
    }

    private static DateTime? ParseDt(string? s) => DateTime.TryParse(s, out var d) ? d : null;

    private static decimal? ParseDec(string? s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                         System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
}

// ── Internal row models ────────────────────────────────────────────────────────

internal class PriceLevelRow
{
    public string    ListID                    { get; set; } = string.Empty;
    public DateTime? TimeCreated               { get; set; }
    public DateTime? TimeModified              { get; set; }
    public string?   EditSequence              { get; set; }
    public string?   Name                      { get; set; }
    public string?   IsActive                  { get; set; }
    public string?   PriceLevelType            { get; set; }
    public string?   PriceLevelFixedPercentage { get; set; }
    public string?   CurrencyRef_ListID        { get; set; }
    public string?   CurrencyRef_FullName      { get; set; }
    public string?   UserData                  { get; set; }
}

internal class PriceLevelPerItemRow
{
    public string    IDKEY              { get; set; } = string.Empty;
    public int       SeqNum             { get; set; }
    public string?   ItemRef_ListID     { get; set; }
    public string?   ItemRef_FullName   { get; set; }
    public decimal?  CustomPrice        { get; set; }
    public string?   CustomPricePercent { get; set; }
    public string?   AdjustPercentage   { get; set; }
    public string?   AdjustRelativeTo   { get; set; }
}

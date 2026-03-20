using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Pulls all UnitOfMeasureSet records from QuickBooks and upserts them into
/// the MySQL `unitofmeasureset` table, keyed on ListID.
/// </summary>
public class UOMSetSync(string mysqlConn, QbSession session)
{
    // ── QBXML request ────────────────────────────────────────────────────────

    private string BuildRequest() => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <?qbxml version="{session.QbXmlVersion}"?>
        <QBXML>
          <QBXMLMsgsRq onError="continueOnError">
            <UnitOfMeasureSetQueryRq requestID="1">
              <ActiveStatus>All</ActiveStatus>
            </UnitOfMeasureSetQueryRq>
          </QBXMLMsgsRq>
        </QBXML>
        """;

    // ── Public entry point ───────────────────────────────────────────────────

    public async Task<SyncResult> RunAsync()
    {
        Console.WriteLine("  Sending UnitOfMeasureSetQueryRq to QuickBooks...");
        var responseXml = session.DoRequests(BuildRequest());

        var rows = ParseResponse(responseXml);
        Console.WriteLine($"  Received {rows.Count} UOM set(s) from QB.");

        if (rows.Count == 0) return new SyncResult();

        return await UpsertAsync(rows);
    }

    // ── XML parsing ──────────────────────────────────────────────────────────

    private static List<UOMSetRow> ParseResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var rs  = doc.Descendants("UnitOfMeasureSetQueryRs").FirstOrDefault()
                  ?? throw new InvalidOperationException("UnitOfMeasureSetQueryRs not found in QB response.");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status != "0")
        {
            var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
            throw new InvalidOperationException($"QB UnitOfMeasureSetQueryRs error {status}: {msg}");
        }

        return rs.Elements("UnitOfMeasureSetRet").Select(e =>
        {
            var baseUnit = e.Element("BaseUnit");
            return new UOMSetRow
            {
                ListID               = Val(e, "ListID") ?? string.Empty,
                TimeCreated          = ParseDt(Val(e, "TimeCreated")),
                TimeModified         = ParseDt(Val(e, "TimeModified")),
                EditSequence         = Val(e, "EditSequence"),
                Name                 = Val(e, "Name"),
                IsActive             = Val(e, "IsActive"),
                UnitOfMeasureType    = Val(e, "UnitOfMeasureType"),
                BaseUnitName         = baseUnit?.Element("Name")?.Value?.Trim(),
                BaseUnitAbbreviation = baseUnit?.Element("Abbreviation")?.Value?.Trim(),
                UserData             = Val(e, "UserData"),
            };
        }).ToList();
    }

    // ── MySQL upsert ─────────────────────────────────────────────────────────

    private async Task<SyncResult> UpsertAsync(List<UOMSetRow> rows)
    {
        const string sql = """
            INSERT INTO unitofmeasureset (
                ListID, TimeCreated, TimeModified, EditSequence,
                Name, IsActive, UnitOfMeasureType,
                BaseUnitName, BaseUnitAbbreviation, UserData
            ) VALUES (
                @ListID, @TimeCreated, @TimeModified, @EditSequence,
                @Name, @IsActive, @UnitOfMeasureType,
                @BaseUnitName, @BaseUnitAbbreviation, @UserData
            )
            ON DUPLICATE KEY UPDATE
                TimeModified         = VALUES(TimeModified),
                EditSequence         = VALUES(EditSequence),
                Name                 = VALUES(Name),
                IsActive             = VALUES(IsActive),
                UnitOfMeasureType    = VALUES(UnitOfMeasureType),
                BaseUnitName         = VALUES(BaseUnitName),
                BaseUnitAbbreviation = VALUES(BaseUnitAbbreviation),
                UserData             = VALUES(UserData)
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

    private static DateTime? ParseDt(string? s) => DateTime.TryParse(s, out var d) ? d : null;
}

// ── Internal row model ────────────────────────────────────────────────────────

internal class UOMSetRow
{
    public string    ListID               { get; set; } = string.Empty;
    public DateTime? TimeCreated          { get; set; }
    public DateTime? TimeModified         { get; set; }
    public string?   EditSequence         { get; set; }
    public string?   Name                 { get; set; }
    public string?   IsActive             { get; set; }
    public string?   UnitOfMeasureType    { get; set; }
    public string?   BaseUnitName         { get; set; }
    public string?   BaseUnitAbbreviation { get; set; }
    public string?   UserData             { get; set; }
}

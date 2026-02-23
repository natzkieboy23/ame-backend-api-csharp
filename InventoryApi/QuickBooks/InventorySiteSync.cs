using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Pulls all InventorySite records from QuickBooks and upserts them into
/// the MySQL `inventorysite` table, keyed on ListID.
/// </summary>
public class InventorySiteSync(string mysqlConn, QbSession session)
{
    // ── QBXML request ────────────────────────────────────────────────────────

    private string BuildRequest() => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <?qbxml version="{session.QbXmlVersion}"?>
        <QBXML>
          <QBXMLMsgsRq onError="continueOnError">
            <InventorySiteQueryRq requestID="1">
              <ActiveStatus>All</ActiveStatus>
            </InventorySiteQueryRq>
          </QBXMLMsgsRq>
        </QBXML>
        """;

    // ── Public entry point ───────────────────────────────────────────────────

    public async Task<SyncResult> RunAsync()
    {
        Console.WriteLine("  Sending InventorySiteQueryRq to QuickBooks...");
        var responseXml = session.DoRequests(BuildRequest());

        var sites = ParseResponse(responseXml);
        Console.WriteLine($"  Received {sites.Count} inventory site(s) from QB.");

        if (sites.Count == 0) return new SyncResult();

        return await UpsertAsync(sites);
    }

    // ── XML parsing ──────────────────────────────────────────────────────────

    private static List<InventorySiteRow> ParseResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var rs  = doc.Descendants("InventorySiteQueryRs").FirstOrDefault()
                  ?? throw new InvalidOperationException("InventorySiteQueryRs not found in QB response.");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status != "0")
        {
            var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
            throw new InvalidOperationException($"QB InventorySiteQueryRs error {status}: {msg}");
        }

        return rs.Elements("InventorySiteRet").Select(e => new InventorySiteRow
        {
            ListID       = Val(e, "ListID") ?? string.Empty,
            TimeCreated  = ParseDt(Val(e, "TimeCreated")),
            TimeModified = ParseDt(Val(e, "TimeModified")),
            EditSequence = Val(e, "EditSequence"),
            Name         = Val(e, "Name"),
            IsActive     = Val(e, "IsActive"),

            ParentSiteRef_ListID   = Val(e, "ParentSiteRef/ListID"),
            ParentSiteRef_FullName = Val(e, "ParentSiteRef/FullName"),

            IsDefaultSite = Val(e, "IsDefaultSite"),
            SiteDesc      = Val(e, "SiteDesc"),
            Contact       = Val(e, "Contact"),
            Phone         = Val(e, "Phone"),
            Fax           = Val(e, "Fax"),
            Email         = Val(e, "Email"),

            SiteAddress_Addr1      = Val(e, "SiteAddress/Addr1"),
            SiteAddress_Addr2      = Val(e, "SiteAddress/Addr2"),
            SiteAddress_Addr3      = Val(e, "SiteAddress/Addr3"),
            SiteAddress_Addr4      = Val(e, "SiteAddress/Addr4"),
            SiteAddress_Addr5      = Val(e, "SiteAddress/Addr5"),
            SiteAddress_City       = Val(e, "SiteAddress/City"),
            SiteAddress_State      = Val(e, "SiteAddress/State"),
            SiteAddress_PostalCode = Val(e, "SiteAddress/PostalCode"),
            SiteAddress_Country    = Val(e, "SiteAddress/Country"),

            SiteAddressBlock_Addr1 = Val(e, "SiteAddressBlock/Addr1"),
            SiteAddressBlock_Addr2 = Val(e, "SiteAddressBlock/Addr2"),
            SiteAddressBlock_Addr3 = Val(e, "SiteAddressBlock/Addr3"),
            SiteAddressBlock_Addr4 = Val(e, "SiteAddressBlock/Addr4"),
            SiteAddressBlock_Addr5 = Val(e, "SiteAddressBlock/Addr5"),

            UserData = Val(e, "UserData"),
        }).ToList();
    }

    // ── MySQL upsert ─────────────────────────────────────────────────────────

    private async Task<SyncResult> UpsertAsync(List<InventorySiteRow> rows)
    {
        const string sql = """
            INSERT INTO inventorysite (
                ListID, TimeCreated, TimeModified, EditSequence,
                Name, IsActive,
                ParentSiteRef_ListID, ParentSiteRef_FullName,
                IsDefaultSite, SiteDesc, Contact, Phone, Fax, Email,
                SiteAddress_Addr1, SiteAddress_Addr2, SiteAddress_Addr3,
                SiteAddress_Addr4, SiteAddress_Addr5,
                SiteAddress_City, SiteAddress_State,
                SiteAddress_PostalCode, SiteAddress_Country,
                SiteAddressBlock_Addr1, SiteAddressBlock_Addr2,
                SiteAddressBlock_Addr3, SiteAddressBlock_Addr4,
                SiteAddressBlock_Addr5,
                UserData
            ) VALUES (
                @ListID, @TimeCreated, @TimeModified, @EditSequence,
                @Name, @IsActive,
                @ParentSiteRef_ListID, @ParentSiteRef_FullName,
                @IsDefaultSite, @SiteDesc, @Contact, @Phone, @Fax, @Email,
                @SiteAddress_Addr1, @SiteAddress_Addr2, @SiteAddress_Addr3,
                @SiteAddress_Addr4, @SiteAddress_Addr5,
                @SiteAddress_City, @SiteAddress_State,
                @SiteAddress_PostalCode, @SiteAddress_Country,
                @SiteAddressBlock_Addr1, @SiteAddressBlock_Addr2,
                @SiteAddressBlock_Addr3, @SiteAddressBlock_Addr4,
                @SiteAddressBlock_Addr5,
                @UserData
            )
            ON DUPLICATE KEY UPDATE
                TimeModified           = VALUES(TimeModified),
                EditSequence           = VALUES(EditSequence),
                Name                   = VALUES(Name),
                IsActive               = VALUES(IsActive),
                ParentSiteRef_ListID   = VALUES(ParentSiteRef_ListID),
                ParentSiteRef_FullName = VALUES(ParentSiteRef_FullName),
                IsDefaultSite          = VALUES(IsDefaultSite),
                SiteDesc               = VALUES(SiteDesc),
                Contact                = VALUES(Contact),
                Phone                  = VALUES(Phone),
                Fax                    = VALUES(Fax),
                Email                  = VALUES(Email),
                SiteAddress_Addr1      = VALUES(SiteAddress_Addr1),
                SiteAddress_Addr2      = VALUES(SiteAddress_Addr2),
                SiteAddress_Addr3      = VALUES(SiteAddress_Addr3),
                SiteAddress_Addr4      = VALUES(SiteAddress_Addr4),
                SiteAddress_Addr5      = VALUES(SiteAddress_Addr5),
                SiteAddress_City       = VALUES(SiteAddress_City),
                SiteAddress_State      = VALUES(SiteAddress_State),
                SiteAddress_PostalCode = VALUES(SiteAddress_PostalCode),
                SiteAddress_Country    = VALUES(SiteAddress_Country),
                SiteAddressBlock_Addr1 = VALUES(SiteAddressBlock_Addr1),
                SiteAddressBlock_Addr2 = VALUES(SiteAddressBlock_Addr2),
                SiteAddressBlock_Addr3 = VALUES(SiteAddressBlock_Addr3),
                SiteAddressBlock_Addr4 = VALUES(SiteAddressBlock_Addr4),
                SiteAddressBlock_Addr5 = VALUES(SiteAddressBlock_Addr5),
                UserData               = VALUES(UserData)
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

internal class InventorySiteRow
{
    public string    ListID       { get; set; } = string.Empty;
    public DateTime? TimeCreated  { get; set; }
    public DateTime? TimeModified { get; set; }
    public string?   EditSequence { get; set; }
    public string?   Name         { get; set; }
    public string?   IsActive     { get; set; }

    public string?   ParentSiteRef_ListID   { get; set; }
    public string?   ParentSiteRef_FullName { get; set; }

    public string?   IsDefaultSite { get; set; }
    public string?   SiteDesc      { get; set; }
    public string?   Contact       { get; set; }
    public string?   Phone         { get; set; }
    public string?   Fax           { get; set; }
    public string?   Email         { get; set; }

    public string?   SiteAddress_Addr1      { get; set; }
    public string?   SiteAddress_Addr2      { get; set; }
    public string?   SiteAddress_Addr3      { get; set; }
    public string?   SiteAddress_Addr4      { get; set; }
    public string?   SiteAddress_Addr5      { get; set; }
    public string?   SiteAddress_City       { get; set; }
    public string?   SiteAddress_State      { get; set; }
    public string?   SiteAddress_PostalCode { get; set; }
    public string?   SiteAddress_Country    { get; set; }

    public string?   SiteAddressBlock_Addr1 { get; set; }
    public string?   SiteAddressBlock_Addr2 { get; set; }
    public string?   SiteAddressBlock_Addr3 { get; set; }
    public string?   SiteAddressBlock_Addr4 { get; set; }
    public string?   SiteAddressBlock_Addr5 { get; set; }

    public string?   UserData { get; set; }
}

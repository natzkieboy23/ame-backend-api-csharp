using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Pulls all active vendors from QuickBooks and upserts them into the
/// MySQL `vendor` table, keyed on ListID.
/// </summary>
public class VendorSync(string mysqlConn, QbSession session)
{
    // ── QBXML request ────────────────────────────────────────────────────────

    private string BuildRequest() => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <?qbxml version="{session.QbXmlVersion}"?>
        <QBXML>
          <QBXMLMsgsRq onError="continueOnError">
            <VendorQueryRq requestID="1">
              <ActiveStatus>All</ActiveStatus>
              <OwnerID>0</OwnerID>
            </VendorQueryRq>
          </QBXMLMsgsRq>
        </QBXML>
        """;

    // ── Public entry point ───────────────────────────────────────────────────

    public async Task<SyncResult> RunAsync()
    {
        Console.WriteLine("  Sending VendorQueryRq to QuickBooks...");
        var responseXml = session.DoRequests(BuildRequest());

        var vendors = ParseResponse(responseXml);
        Console.WriteLine($"  Received {vendors.Count} vendor(s) from QB.");

        if (vendors.Count == 0) return new SyncResult();

        return await UpsertAsync(vendors);
    }

    // ── XML parsing ──────────────────────────────────────────────────────────

    private static List<VendorRow> ParseResponse(string xml)
    {
        var doc  = XDocument.Parse(xml);
        var rs   = doc.Descendants("VendorQueryRs").FirstOrDefault()
                   ?? throw new InvalidOperationException("VendorQueryRs element not found in QB response.");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status != "0")
        {
            var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
            throw new InvalidOperationException($"QB VendorQueryRs error {status}: {msg}");
        }

        return rs.Elements("VendorRet").Select(v => new VendorRow
        {
            ListID        = Val(v, "ListID") ?? string.Empty,
            TimeCreated   = ParseDt(Val(v, "TimeCreated")),
            TimeModified  = ParseDt(Val(v, "TimeModified")),
            EditSequence  = Val(v, "EditSequence"),
            Name          = Val(v, "Name"),
            IsActive      = Val(v, "IsActive"),
            CompanyName   = Val(v, "CompanyName"),
            Salutation    = Val(v, "Salutation"),
            FirstName     = Val(v, "FirstName"),
            MiddleName    = Val(v, "MiddleName"),
            LastName      = Val(v, "LastName"),
            Suffix        = Val(v, "Suffix"),
            JobTitle      = Val(v, "JobTitle"),
            Phone         = Val(v, "Phone"),
            Mobile        = Val(v, "Mobile"),
            AltPhone      = Val(v, "AltPhone"),
            Fax           = Val(v, "Fax"),
            Email         = Val(v, "Email"),
            Contact       = Val(v, "Contact"),
            AltContact    = Val(v, "AltContact"),
            NameOnCheck   = Val(v, "NameOnCheck"),
            Notes         = Val(v, "Notes"),
            AccountNumber = Val(v, "AccountNumber"),

            VendorAddress_Addr1      = Val(v, "VendorAddress/Addr1"),
            VendorAddress_Addr2      = Val(v, "VendorAddress/Addr2"),
            VendorAddress_Addr3      = Val(v, "VendorAddress/Addr3"),
            VendorAddress_City       = Val(v, "VendorAddress/City"),
            VendorAddress_State      = Val(v, "VendorAddress/State"),
            VendorAddress_PostalCode = Val(v, "VendorAddress/PostalCode"),
            VendorAddress_Country    = Val(v, "VendorAddress/Country"),

            VendorTypeRef_ListID   = Val(v, "VendorTypeRef/ListID"),
            VendorTypeRef_FullName = Val(v, "VendorTypeRef/FullName"),
            TermsRef_ListID        = Val(v, "TermsRef/ListID"),
            TermsRef_FullName      = Val(v, "TermsRef/FullName"),
            CurrencyRef_ListID     = Val(v, "CurrencyRef/ListID"),
            CurrencyRef_FullName   = Val(v, "CurrencyRef/FullName"),
            ClassRef_ListID        = Val(v, "ClassRef/ListID"),
            ClassRef_FullName      = Val(v, "ClassRef/FullName"),

            CreditLimit             = ParseDec(Val(v, "CreditLimit")),
            Balance                 = ParseDec(Val(v, "Balance")),
            VendorTaxIdent          = Val(v, "VendorTaxIdent"),
            IsVendorEligibleFor1099 = Val(v, "IsVendorEligibleFor1099"),
        }).ToList();
    }

    // ── MySQL upsert ─────────────────────────────────────────────────────────

    private async Task<SyncResult> UpsertAsync(List<VendorRow> rows)
    {
        const string sql = """
            INSERT INTO vendor (
                ListID, TimeCreated, TimeModified, EditSequence,
                Name, IsActive, CompanyName, Salutation, FirstName, MiddleName,
                LastName, Suffix, JobTitle, Phone, Mobile, AltPhone, Fax, Email,
                Contact, AltContact, NameOnCheck, Notes, AccountNumber,
                VendorAddress_Addr1, VendorAddress_Addr2, VendorAddress_Addr3,
                VendorAddress_City, VendorAddress_State, VendorAddress_PostalCode,
                VendorAddress_Country,
                VendorTypeRef_ListID, VendorTypeRef_FullName,
                TermsRef_ListID, TermsRef_FullName,
                CurrencyRef_ListID, CurrencyRef_FullName,
                ClassRef_ListID, ClassRef_FullName,
                CreditLimit, Balance, VendorTaxIdent, IsVendorEligibleFor1099
            ) VALUES (
                @ListID, @TimeCreated, @TimeModified, @EditSequence,
                @Name, @IsActive, @CompanyName, @Salutation, @FirstName, @MiddleName,
                @LastName, @Suffix, @JobTitle, @Phone, @Mobile, @AltPhone, @Fax, @Email,
                @Contact, @AltContact, @NameOnCheck, @Notes, @AccountNumber,
                @VendorAddress_Addr1, @VendorAddress_Addr2, @VendorAddress_Addr3,
                @VendorAddress_City, @VendorAddress_State, @VendorAddress_PostalCode,
                @VendorAddress_Country,
                @VendorTypeRef_ListID, @VendorTypeRef_FullName,
                @TermsRef_ListID, @TermsRef_FullName,
                @CurrencyRef_ListID, @CurrencyRef_FullName,
                @ClassRef_ListID, @ClassRef_FullName,
                @CreditLimit, @Balance, @VendorTaxIdent, @IsVendorEligibleFor1099
            )
            ON DUPLICATE KEY UPDATE
                TimeModified             = VALUES(TimeModified),
                EditSequence             = VALUES(EditSequence),
                Name                     = VALUES(Name),
                IsActive                 = VALUES(IsActive),
                CompanyName              = VALUES(CompanyName),
                Salutation               = VALUES(Salutation),
                FirstName                = VALUES(FirstName),
                MiddleName               = VALUES(MiddleName),
                LastName                 = VALUES(LastName),
                Suffix                   = VALUES(Suffix),
                JobTitle                 = VALUES(JobTitle),
                Phone                    = VALUES(Phone),
                Mobile                   = VALUES(Mobile),
                AltPhone                 = VALUES(AltPhone),
                Fax                      = VALUES(Fax),
                Email                    = VALUES(Email),
                Contact                  = VALUES(Contact),
                AltContact               = VALUES(AltContact),
                NameOnCheck              = VALUES(NameOnCheck),
                Notes                    = VALUES(Notes),
                AccountNumber            = VALUES(AccountNumber),
                VendorAddress_Addr1      = VALUES(VendorAddress_Addr1),
                VendorAddress_Addr2      = VALUES(VendorAddress_Addr2),
                VendorAddress_Addr3      = VALUES(VendorAddress_Addr3),
                VendorAddress_City       = VALUES(VendorAddress_City),
                VendorAddress_State      = VALUES(VendorAddress_State),
                VendorAddress_PostalCode = VALUES(VendorAddress_PostalCode),
                VendorAddress_Country    = VALUES(VendorAddress_Country),
                VendorTypeRef_ListID     = VALUES(VendorTypeRef_ListID),
                VendorTypeRef_FullName   = VALUES(VendorTypeRef_FullName),
                TermsRef_ListID          = VALUES(TermsRef_ListID),
                TermsRef_FullName        = VALUES(TermsRef_FullName),
                CurrencyRef_ListID       = VALUES(CurrencyRef_ListID),
                CurrencyRef_FullName     = VALUES(CurrencyRef_FullName),
                ClassRef_ListID          = VALUES(ClassRef_ListID),
                ClassRef_FullName        = VALUES(ClassRef_FullName),
                CreditLimit              = VALUES(CreditLimit),
                Balance                  = VALUES(Balance),
                VendorTaxIdent           = VALUES(VendorTaxIdent),
                IsVendorEligibleFor1099  = VALUES(IsVendorEligibleFor1099)
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        await conn.OpenAsync();

        int affected = 0;
        foreach (var row in rows)
            affected += await conn.ExecuteAsync(sql, row);

        // MySQL: INSERT → 1 row affected, ON DUPLICATE KEY UPDATE → 2 rows affected
        int updated  = rows.Count(r => affected >= 2);  // approximate
        int inserted = rows.Count - updated;
        return new SyncResult { Total = rows.Count, Inserted = inserted, Updated = updated };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? Val(XElement el, string path)
    {
        var parts = path.Split('/');
        XElement? cur = el;
        foreach (var p in parts)
        {
            cur = cur?.Element(p);
            if (cur is null) return null;
        }
        return string.IsNullOrWhiteSpace(cur?.Value) ? null : cur.Value.Trim();
    }

    private static DateTime? ParseDt(string? s)
        => DateTime.TryParse(s, out var dt) ? dt : null;

    private static decimal? ParseDec(string? s)
        => decimal.TryParse(s, out var d) ? d : null;
}

// ── Internal row model ────────────────────────────────────────────────────────

internal class VendorRow
{
    public string  ListID        { get; set; } = string.Empty;
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }
    public string? EditSequence  { get; set; }
    public string? Name          { get; set; }
    public string? IsActive      { get; set; }
    public string? CompanyName   { get; set; }
    public string? Salutation    { get; set; }
    public string? FirstName     { get; set; }
    public string? MiddleName    { get; set; }
    public string? LastName      { get; set; }
    public string? Suffix        { get; set; }
    public string? JobTitle      { get; set; }
    public string? Phone         { get; set; }
    public string? Mobile        { get; set; }
    public string? AltPhone      { get; set; }
    public string? Fax           { get; set; }
    public string? Email         { get; set; }
    public string? Contact       { get; set; }
    public string? AltContact    { get; set; }
    public string? NameOnCheck   { get; set; }
    public string? Notes         { get; set; }
    public string? AccountNumber { get; set; }
    public string? VendorAddress_Addr1      { get; set; }
    public string? VendorAddress_Addr2      { get; set; }
    public string? VendorAddress_Addr3      { get; set; }
    public string? VendorAddress_City       { get; set; }
    public string? VendorAddress_State      { get; set; }
    public string? VendorAddress_PostalCode { get; set; }
    public string? VendorAddress_Country    { get; set; }
    public string? VendorTypeRef_ListID     { get; set; }
    public string? VendorTypeRef_FullName   { get; set; }
    public string? TermsRef_ListID          { get; set; }
    public string? TermsRef_FullName        { get; set; }
    public string? CurrencyRef_ListID       { get; set; }
    public string? CurrencyRef_FullName     { get; set; }
    public string? ClassRef_ListID          { get; set; }
    public string? ClassRef_FullName        { get; set; }
    public decimal? CreditLimit             { get; set; }
    public decimal? Balance                 { get; set; }
    public string?  VendorTaxIdent          { get; set; }
    public string?  IsVendorEligibleFor1099 { get; set; }
}

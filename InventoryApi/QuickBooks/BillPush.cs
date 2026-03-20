using System.Text;
using System.Xml.Linq;
using Dapper;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Reads all pending Bills from MySQL and pushes them to QuickBooks
/// as a single batch BillAdd request.
///
/// On success each bill's Status is updated to "Synced" and the QB-assigned
/// TxnID is stored in ExternalGUID for cross-reference.
/// </summary>
public class BillPush(string mysqlConn, QbSession session)
{
    // ── Public entry point ───────────────────────────────────────────────────

    public async Task RunAsync()
    {
        var pending = await GetPendingBillsAsync();
        if (pending.Count == 0)
        {
            Ui.Warn("  No pending bills found (Status = 'ADD').");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  Found {pending.Count} pending bill(s):");
        Console.WriteLine();
        Console.WriteLine($"  {"#",-4} {"TxnID",-38} {"Vendor",-30} {"Date",-12} {"Lines"}");
        Console.WriteLine("  " + new string('─', 90));
        for (int i = 0; i < pending.Count; i++)
        {
            var b = pending[i];
            Console.WriteLine($"  {i + 1,-4} {b.TxnID,-38} {Trunc(b.VendorRef_FullName, 30),-30} " +
                              $"{b.TxnDate?.ToString("yyyy-MM-dd"),-12} {b.LineCount}");
        }

        Console.WriteLine();
        Console.Write($"  Push all {pending.Count} bill(s) to QuickBooks? (y/n): ");
        var answer = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
        if (answer != "y" && answer != "yes")
        {
            Console.WriteLine("  Cancelled.");
            return;
        }

        // Load all line items for each bill
        var allLines = new Dictionary<string, List<LineRow>>();
        foreach (var bill in pending)
        {
            var lines = await GetLinesAsync(bill.TxnID);
            if (lines.Count == 0)
            {
                Ui.Warn($"  Skipping {bill.TxnID} — no line items.");
                continue;
            }
            allLines[bill.TxnID] = lines;
        }

        if (allLines.Count == 0)
        {
            Ui.Warn("  No bills with line items to push.");
            return;
        }

        // Resolve the "Piece" UOM set — stamped onto every pending line before sync
        var piecesUom = await GetPiecesUomAsync();
        if (piecesUom is null)
            Ui.Warn("  Warning: 'Piece' UOM set not found — OverrideUOMSetRef will not be updated.");
        else
            Console.WriteLine($"  Piece UOM: {piecesUom.FullName} ({piecesUom.ListID}, base unit: {piecesUom.BaseUnitName})");

        // Stamp Piece UOM onto every pending line in DB before syncing
        if (piecesUom is not null)
        {
            var pendingTxnIds = allLines.Keys.ToList();
            await UpdatePendingLinesUomAsync(pendingTxnIds, piecesUom);
            Console.WriteLine($"  Updated line(s) → OverrideUOMSetRef = Piece.");
        }

        // Send one ProcessRequest per bill — isolates parse errors and lets others proceed
        Console.WriteLine();
        int successCount = 0;
        int failCount    = 0;

        foreach (var bill in pending)
        {
            if (!allLines.TryGetValue(bill.TxnID, out var lines)) continue;

            var requestXml = BuildSingleBillAddRq(bill, lines, piecesUom);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  ── {Trunc(bill.VendorRef_FullName, 40)} ──────────────────────────");
            Console.WriteLine(requestXml);
            Console.WriteLine("  ────────────────────────────────────────────────────\n");
            Console.ResetColor();

            string responseXml;
            try
            {
                responseXml = session.DoRequests(requestXml);
            }
            catch (Exception ex)
            {
                Ui.Error($"  ✕ {Trunc(bill.VendorRef_FullName, 30),-30} → QB parse/COM error: {ex.Message}");
                failCount++;
                continue;
            }

            var (qbTxnId, error) = ParseSingleBillAddRs(responseXml);
            if (error is null)
            {
                await MarkSyncedAsync(bill.TxnID, qbTxnId!);
                Ui.Ok($"  ✓ {Trunc(bill.VendorRef_FullName, 30),-30} → QB TxnID: {qbTxnId}");
                successCount++;
            }
            else
            {
                Ui.Error($"  ✕ {Trunc(bill.VendorRef_FullName, 30),-30} → {error}");
                failCount++;
            }
        }

        Console.WriteLine();
        if (successCount > 0) Ui.Ok($"  Batch complete: {successCount} synced, {failCount} failed.");
        else                  Ui.Error($"  Batch complete: all {failCount} failed.");
    }

    // ── QBXML single-bill builder ─────────────────────────────────────────────

    private string BuildSingleBillAddRq(BillSummary bill, List<LineRow> lines, UomRow? piecesUom)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0"?>""");
        sb.AppendLine($"""<?qbxml version="{session.QbXmlVersion}"?>""");
        sb.AppendLine("""<QBXML>""");
        sb.AppendLine("""  <QBXMLMsgsRq onError="continueOnError">""");
        AppendBillAddRq(sb, bill, lines, 1, piecesUom);
        sb.AppendLine("""  </QBXMLMsgsRq>""");
        sb.AppendLine("""</QBXML>""");
        return sb.ToString();
    }

    private void AppendBillAddRq(
        StringBuilder sb,
        BillSummary bill,
        List<LineRow> lines,
        int requestId,
        UomRow? piecesUom)
    {
        sb.AppendLine($"""    <BillAddRq requestID="{requestId}">""");
        sb.AppendLine("""      <BillAdd>""");

        if (!string.IsNullOrWhiteSpace(bill.VendorRef_ListID))
        {
            sb.AppendLine("""        <VendorRef>""");
            sb.AppendLine($"""          <ListID>{Escape(bill.VendorRef_ListID)}</ListID>""");
            sb.AppendLine("""        </VendorRef>""");
        }
        else if (!string.IsNullOrWhiteSpace(bill.VendorRef_FullName))
        {
            sb.AppendLine("""        <VendorRef>""");
            sb.AppendLine($"""          <FullName>{Escape(bill.VendorRef_FullName)}</FullName>""");
            sb.AppendLine("""        </VendorRef>""");
        }

        if (!string.IsNullOrWhiteSpace(bill.APAccountRef_ListID))
        {
            sb.AppendLine("""        <APAccountRef>""");
            sb.AppendLine($"""          <ListID>{Escape(bill.APAccountRef_ListID)}</ListID>""");
            sb.AppendLine("""        </APAccountRef>""");
        }

        if (bill.TxnDate.HasValue)
            sb.AppendLine($"""        <TxnDate>{bill.TxnDate.Value:yyyy-MM-dd}</TxnDate>""");

        if (bill.DueDate.HasValue)
            sb.AppendLine($"""        <DueDate>{bill.DueDate.Value:yyyy-MM-dd}</DueDate>""");

        if (!string.IsNullOrWhiteSpace(bill.RefNumber))
            sb.AppendLine($"""        <RefNumber>{Escape(bill.RefNumber)}</RefNumber>""");

        if (!string.IsNullOrWhiteSpace(bill.TermsRef_ListID))
        {
            sb.AppendLine("""        <TermsRef>""");
            sb.AppendLine($"""          <ListID>{Escape(bill.TermsRef_ListID)}</ListID>""");
            sb.AppendLine("""        </TermsRef>""");
        }

        if (!string.IsNullOrWhiteSpace(bill.Memo))
            sb.AppendLine($"""        <Memo>{Escape(bill.Memo)}</Memo>""");

        foreach (var line in lines)
        {
            sb.AppendLine("""        <ItemLineAdd>""");

            if (!string.IsNullOrWhiteSpace(line.ItemRef_ListID))
            {
                sb.AppendLine("""          <ItemRef>""");
                sb.AppendLine($"""            <ListID>{Escape(line.ItemRef_ListID)}</ListID>""");
                sb.AppendLine("""          </ItemRef>""");
            }
            else if (!string.IsNullOrWhiteSpace(line.ItemRef_FullName))
            {
                sb.AppendLine("""          <ItemRef>""");
                sb.AppendLine($"""            <FullName>{Escape(line.ItemRef_FullName)}</FullName>""");
                sb.AppendLine("""          </ItemRef>""");
            }

            if (!string.IsNullOrWhiteSpace(line.Description))
                sb.AppendLine($"""          <Desc>{Escape(line.Description)}</Desc>""");

            if (line.Quantity.HasValue)
                sb.AppendLine($"""          <Quantity>{line.Quantity.Value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}</Quantity>""");

            // Use the unit resolved from the item's own UOM set (via GetLinesAsync COALESCE join).
            // OverrideUOMSetRef is not valid in BillAdd > ItemLineAdd (causes QB parse error).
            if (!string.IsNullOrWhiteSpace(line.UnitOfMeasure))
                sb.AppendLine($"""          <UnitOfMeasure>{Escape(line.UnitOfMeasure)}</UnitOfMeasure>""");

            // Amount is omitted — QB computes it from Quantity × Cost automatically
            if (!string.IsNullOrWhiteSpace(line.Cost) &&
                decimal.TryParse(line.Cost, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out var costVal))
                sb.AppendLine($"""          <Cost>{costVal.ToString(System.Globalization.CultureInfo.InvariantCulture)}</Cost>""");

            if (!string.IsNullOrWhiteSpace(line.ClassRef_ListID))
            {
                sb.AppendLine("""          <ClassRef>""");
                sb.AppendLine($"""            <ListID>{Escape(line.ClassRef_ListID)}</ListID>""");
                sb.AppendLine("""          </ClassRef>""");
            }

            if (!string.IsNullOrWhiteSpace(line.SerialNumber))
                sb.AppendLine($"""          <SerialNumber>{Escape(line.SerialNumber)}</SerialNumber>""");

            if (!string.IsNullOrWhiteSpace(line.LotNumber))
                sb.AppendLine($"""          <LotNumber>{Escape(line.LotNumber)}</LotNumber>""");

            sb.AppendLine("""        </ItemLineAdd>""");
        }

        sb.AppendLine("""      </BillAdd>""");
        sb.AppendLine("""    </BillAddRq>""");
    }

    // ── QBXML single-bill response parser ────────────────────────────────────

    /// <summary>Returns (qbTxnId, errorMessage) for a single BillAddRs response.</summary>
    private static (string? qbTxnId, string? error) ParseSingleBillAddRs(string xml)
    {
        var doc    = XDocument.Parse(xml);
        var rs     = doc.Descendants("BillAddRs").FirstOrDefault();
        if (rs is null) return (null, "No BillAddRs element in QB response");

        var status = (string?)rs.Attribute("statusCode") ?? "-1";
        if (status == "0")
        {
            var qbTxnId = rs.Element("BillRet")?.Element("TxnID")?.Value ?? "unknown";
            return (qbTxnId, null);
        }

        var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
        return (null, $"QB error {status}: {msg}");
    }

    // ── MySQL helpers ─────────────────────────────────────────────────────────

    private async Task<List<BillSummary>> GetPendingBillsAsync()
    {
        const string sql = """
            SELECT
                b.TxnID, b.VendorRef_ListID, b.VendorRef_FullName,
                b.APAccountRef_ListID, b.TermsRef_ListID,
                b.TxnDate, b.DueDate, b.RefNumber, b.Memo, b.Status,
                COUNT(l.TxnLineID) AS LineCount
            FROM bill b
            LEFT JOIN txnitemlinedetail l ON l.IDKEY = b.TxnID
            WHERE b.Status = 'ADD'
            GROUP BY b.TxnID
            ORDER BY b.TxnDate DESC
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        return (await conn.QueryAsync<BillSummary>(sql)).ToList();
    }

    private async Task<List<LineRow>> GetLinesAsync(string txnId)
    {
        const string sql = """
            SELECT l.TxnLineID, l.IDKEY, l.SeqNum,
                   l.ItemRef_ListID, l.ItemRef_FullName,
                   l.Description, l.Quantity,
                   COALESCE(NULLIF(l.UnitOfMeasure, ''), u.BaseUnitName) AS UnitOfMeasure,
                   l.Cost, l.Amount,
                   l.InventorySiteRef_ListID, l.InventorySiteRef_FullName,
                   l.SerialNumber, l.LotNumber,
                   l.ClassRef_ListID, l.ClassRef_FullName
            FROM txnitemlinedetail l
            LEFT JOIN iteminventory i ON i.ListID = l.ItemRef_ListID
            LEFT JOIN unitofmeasureset u ON u.ListID = i.UnitOfMeasureSetRef_ListID
            WHERE l.IDKEY = @TxnID
            ORDER BY l.SeqNum
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        return (await conn.QueryAsync<LineRow>(sql, new { TxnID = txnId })).ToList();
    }

    private async Task UpdatePendingLinesUomAsync(List<string> txnIds, UomRow piecesUom)
    {
        const string sql = """
            UPDATE txnitemlinedetail
            SET OverrideUOMSetRef_ListID   = @ListID,
                OverrideUOMSetRef_FullName = @FullName
            WHERE IDKEY IN @TxnIds
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        await conn.ExecuteAsync(sql, new
        {
            piecesUom.ListID,
            piecesUom.FullName,
            TxnIds = txnIds
        });
    }

    private async Task<UomRow?> GetPiecesUomAsync()
    {
        const string sql = """
            SELECT ListID, Name AS FullName, BaseUnitName
            FROM unitofmeasureset
            WHERE Name = 'Piece'
            LIMIT 1
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        return await conn.QueryFirstOrDefaultAsync<UomRow>(sql);
    }

    private async Task MarkSyncedAsync(string txnId, string qbTxnId)
    {
        const string sql = """
            UPDATE bill
            SET Status = 'Synced', ExternalGUID = @QbTxnId, TimeModified = @Now
            WHERE TxnID = @TxnID
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        await conn.ExecuteAsync(sql, new { TxnID = txnId, QbTxnId = qbTxnId, Now = DateTime.UtcNow });
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    // QB's COM XML parser rejects &apos; but accepts &#39; (numeric character reference).
    private static string Escape(string? s) =>
        (s ?? string.Empty)
            .Replace("&",  "&amp;")
            .Replace("<",  "&lt;")
            .Replace(">",  "&gt;")
            .Replace("'",  "&#39;");

    private static string Trunc(string? s, int max) =>
        s is null ? string.Empty :
        s.Length <= max ? s : s[..(max - 1)] + "…";
}

// ── Internal row models ────────────────────────────────────────────────────────

internal class BillSummary
{
    public string    TxnID               { get; set; } = string.Empty;
    public string?   VendorRef_ListID    { get; set; }
    public string?   VendorRef_FullName  { get; set; }
    public string?   APAccountRef_ListID { get; set; }
    public string?   TermsRef_ListID     { get; set; }
    public DateTime? TxnDate             { get; set; }
    public DateTime? DueDate             { get; set; }
    public string?   RefNumber           { get; set; }
    public string?   Memo                { get; set; }
    public string?   Status              { get; set; }
    public int       LineCount           { get; set; }
}

internal class UomRow
{
    public string ListID       { get; set; } = string.Empty;
    public string FullName     { get; set; } = string.Empty;
    public string BaseUnitName { get; set; } = string.Empty;
}

internal class LineRow
{
    public string    TxnLineID                 { get; set; } = string.Empty;
    public string    IDKEY                     { get; set; } = string.Empty;
    public int?      SeqNum                    { get; set; }
    public string?   ItemRef_ListID            { get; set; }
    public string?   ItemRef_FullName          { get; set; }
    public string?   Description               { get; set; }
    public decimal?  Quantity                  { get; set; }
    public string?   UnitOfMeasure             { get; set; }
    public string?   Cost                      { get; set; }
    public decimal?  Amount                    { get; set; }
    public string?   InventorySiteRef_ListID   { get; set; }
    public string?   InventorySiteRef_FullName { get; set; }
    public string?   SerialNumber              { get; set; }
    public string?   LotNumber                 { get; set; }
    public string?   ClassRef_ListID           { get; set; }
    public string?   ClassRef_FullName         { get; set; }
}

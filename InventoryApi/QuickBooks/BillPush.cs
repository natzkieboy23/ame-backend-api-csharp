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

        // Build and send single batch QBXML request
        Console.WriteLine($"\n  Building batch BillAddRq for {allLines.Count} bill(s)...");
        var requestXml = BuildBatchBillAddRq(pending, allLines);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n  ── XML being sent ──────────────────────────────────");
        Console.WriteLine(requestXml);
        Console.WriteLine("  ────────────────────────────────────────────────────\n");
        Console.ResetColor();

        var responseXml = session.DoRequests(requestXml);

        // Parse all BillAddRs responses
        var results = ParseBatchBillAddRs(responseXml, pending, allLines);

        // Report and persist each result
        int successCount = 0;
        int failCount    = 0;

        foreach (var (txnId, qbTxnId, error) in results)
        {
            if (error is null)
            {
                await MarkSyncedAsync(txnId, qbTxnId!);
                var vendor = pending.First(b => b.TxnID == txnId).VendorRef_FullName;
                Ui.Ok($"  ✓ {Trunc(vendor, 30),-30} → QB TxnID: {qbTxnId}");
                successCount++;
            }
            else
            {
                var vendor = pending.First(b => b.TxnID == txnId).VendorRef_FullName;
                Ui.Error($"  ✕ {Trunc(vendor, 30),-30} → {error}");
                failCount++;
            }
        }

        Console.WriteLine();
        if (successCount > 0) Ui.Ok($"  Batch complete: {successCount} synced, {failCount} failed.");
        else                  Ui.Error($"  Batch complete: all {failCount} failed.");
    }

    // ── QBXML batch builder ───────────────────────────────────────────────────

    private string BuildBatchBillAddRq(
        List<BillSummary> bills,
        Dictionary<string, List<LineRow>> allLines)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"""<?xml version="1.0" encoding="utf-8"?>""");
        sb.AppendLine($"""<?qbxml version="{session.QbXmlVersion}"?>""");
        sb.AppendLine("""<QBXML>""");
        sb.AppendLine("""  <QBXMLMsgsRq onError="continueOnError">""");

        int requestId = 1;
        foreach (var bill in bills)
        {
            if (!allLines.TryGetValue(bill.TxnID, out var lines)) continue;
            AppendBillAddRq(sb, bill, lines, requestId++);
        }

        sb.AppendLine("""  </QBXMLMsgsRq>""");
        sb.AppendLine("""</QBXML>""");
        return sb.ToString();
    }

    private void AppendBillAddRq(
        StringBuilder sb,
        BillSummary bill,
        List<LineRow> lines,
        int requestId)
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

    // ── QBXML batch response parser ───────────────────────────────────────────

    /// <summary>Returns (mysqlTxnId, qbTxnId, errorMessage) per bill.</summary>
    private static List<(string txnId, string? qbTxnId, string? error)> ParseBatchBillAddRs(
        string xml,
        List<BillSummary> bills,
        Dictionary<string, List<LineRow>> allLines)
    {
        var doc     = XDocument.Parse(xml);
        var results = new List<(string, string?, string?)>();

        // Build ordered list of bills that were actually sent (same order as requestIDs)
        var sentBills = bills
            .Where(b => allLines.ContainsKey(b.TxnID))
            .ToList();

        var rsElements = doc.Descendants("BillAddRs").ToList();

        for (int i = 0; i < rsElements.Count; i++)
        {
            var rs     = rsElements[i];
            var txnId  = i < sentBills.Count ? sentBills[i].TxnID : $"unknown-{i}";
            var status = (string?)rs.Attribute("statusCode") ?? "-1";

            if (status == "0")
            {
                var ret      = rs.Element("BillRet");
                var qbTxnId  = ret?.Element("TxnID")?.Value ?? "unknown";
                results.Add((txnId, qbTxnId, null));
            }
            else
            {
                var msg = (string?)rs.Attribute("statusMessage") ?? "Unknown QB error";
                results.Add((txnId, null, $"QB error {status}: {msg}"));
            }
        }

        return results;
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
            SELECT TxnLineID, IDKEY, SeqNum,
                   ItemRef_ListID, ItemRef_FullName,
                   Description, Quantity, UnitOfMeasure,
                   Cost, Amount,
                   InventorySiteRef_ListID, InventorySiteRef_FullName,
                   SerialNumber, LotNumber,
                   ClassRef_ListID, ClassRef_FullName
            FROM txnitemlinedetail
            WHERE IDKEY = @TxnID
            ORDER BY SeqNum
            """;

        await using var conn = new MySqlConnection(mysqlConn);
        return (await conn.QueryAsync<LineRow>(sql, new { TxnID = txnId })).ToList();
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

    private static string Escape(string? s) =>
        (s ?? string.Empty)
            .Replace("&",  "&amp;")
            .Replace("<",  "&lt;")
            .Replace(">",  "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'",  "&apos;");

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

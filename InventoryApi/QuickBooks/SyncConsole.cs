using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Entry point for the QuickBooks sync console mode.
/// Invoked when the app is started with the --sync argument:
///   dotnet run -- --sync
/// </summary>
public static class SyncConsole
{
    public static async Task RunAsync()
    {
        // ── Load configuration ─────────────────────────────────────────────────

        string mysqlConn;
        QuickBooksConfig qbConfig;
        try
        {
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            mysqlConn = cfg.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Missing 'ConnectionStrings:DefaultConnection' in appsettings.json.");

            qbConfig = cfg.GetSection("QuickBooks").Get<QuickBooksConfig>()
                ?? throw new InvalidOperationException(
                    "Missing 'QuickBooks' section in appsettings.json.");
        }
        catch (Exception ex)
        {
            Ui.Error($"Configuration error: {ex.Message}");
            return;
        }

        // ── Connection state (persists across menu refreshes) ──────────────────

        bool?  qbConnected  = null;
        string qbStatusText = "Not tested";

        bool?  dbConnected  = null;
        string dbStatusText = "Not tested";

        // ── Startup: test both connections ─────────────────────────────────────

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  AME · QuickBooks Sync Console\n");
        Console.ResetColor();

        // DB test first (fast, no dialogs)
        Console.Write("  Checking database...      ");
        (dbConnected, dbStatusText) = await TryConnectDbAsync(mysqlConn);
        PrintStatus(dbConnected, dbStatusText);

        // QB test (may trigger authorization dialog)
        Console.Write("  Checking QuickBooks...    ");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  (If a QuickBooks dialog appears, approve it to grant access.)");
        Console.ResetColor();
        (qbConnected, qbStatusText) = TryConnectQb(qbConfig);
        Console.Write("  QuickBooks:               ");
        PrintStatus(qbConnected, qbStatusText);

        Console.WriteLine();
        Console.Write("  Press any key to continue...");
        Console.ReadKey(intercept: true);

        // ── Main loop ──────────────────────────────────────────────────────────

        while (true)
        {
            PrintMenu(qbConfig, qbConnected, qbStatusText, dbConnected, dbStatusText);

            var key = Console.ReadKey(intercept: true).KeyChar;
            Console.WriteLine();

            if (key == '0') break;

            if (key == '6')
            {
                Console.WriteLine();
                Console.WriteLine("  Re-testing connections...\n");

                Console.Write("  Database:    ");
                (dbConnected, dbStatusText) = await TryConnectDbAsync(mysqlConn);
                PrintStatus(dbConnected, dbStatusText);

                Console.Write("  QuickBooks:  ");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  (If a QuickBooks dialog appears, approve it to grant access.)");
                Console.ResetColor();
                (qbConnected, qbStatusText) = TryConnectQb(qbConfig);
                Console.Write("  QuickBooks:  ");
                PrintStatus(qbConnected, qbStatusText);
            }
            else if (key is '1' or '2' or '3' or '4' or '5')
            {
                await RunOptionAsync(key, mysqlConn, qbConfig);
                // Silently refresh statuses after each operation
                (dbConnected, dbStatusText) = await TryConnectDbAsync(mysqlConn);
                (qbConnected, qbStatusText) = TryConnectQb(qbConfig);
            }
            else
            {
                Ui.Warn("  Invalid option. Press 1–6 or 0.");
            }

            Console.WriteLine();
            Console.Write("  Press any key to return to menu...");
            Console.ReadKey(intercept: true);
        }

        Console.WriteLine("\n  Goodbye.\n");
    }

    // ── Connection testers ─────────────────────────────────────────────────────

    private static async Task<(bool ok, string message)> TryConnectDbAsync(string mysqlConn)
    {
        try
        {
            await using var conn = new MySqlConnection(mysqlConn);
            await conn.OpenAsync();
            var row = await conn.QuerySingleAsync<(string db, string version)>(
                "SELECT DATABASE() AS db, VERSION() AS version");
            return (true, $"● {row.db}  @  {conn.DataSource}  (MySQL {row.version})");
        }
        catch (Exception ex)
        {
            return (false, $"✕ {ex.Message}");
        }
    }

    private static (bool ok, string message) TryConnectQb(QuickBooksConfig cfg)
    {
        try
        {
            using var session = new QbSession(cfg);
            session.Open();
            return (true, $"● Connected  ({cfg.AppName}  ·  QBXML v{cfg.QbXmlVersion})");
        }
        catch (InvalidOperationException ex)
        {
            return (false, $"✕ {ex.Message}");
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return (false, $"✕ {msg}");
        }
    }

    // ── Menu printer ───────────────────────────────────────────────────────────

    private static void PrintMenu(
        QuickBooksConfig cfg,
        bool? qbOk,  string qbText,
        bool? dbOk,  string dbText)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("""

  ╔══════════════════════════════════════════════════╗
  ║       AME  ·  QuickBooks Sync Console            ║
  ╚══════════════════════════════════════════════════╝
  """);
        Console.ResetColor();

        // ── Status panel ──────────────────────────────────────────────────────
        Console.Write("  Database    : ");
        PrintStatus(dbOk, dbText);

        Console.Write("  QuickBooks  : ");
        PrintStatus(qbOk, qbText);

        Console.WriteLine();
        Console.WriteLine("  ─── Pull from QuickBooks ─────────────────────────");
        Console.WriteLine("  [1]  Sync Vendors          (QB → MySQL vendor table)");
        Console.WriteLine("  [2]  Sync Items            (QB → MySQL iteminventory table)");
        Console.WriteLine("  [3]  Sync Inventory Sites  (QB → MySQL inventorysite table)");
        Console.WriteLine("  [4]  Sync Item Sites (Qty) (QB → MySQL itemsites table)");
        Console.WriteLine();
        Console.WriteLine("  ─── Push to QuickBooks ───────────────────────────");
        Console.WriteLine("  [5]  Send Bills            (batch sync all pending → QB BillAdd)");
        Console.WriteLine();
        Console.WriteLine("  ─── Connection ───────────────────────────────────");
        Console.WriteLine("  [6]  Test Connections");
        Console.WriteLine();
        Console.WriteLine("  ──────────────────────────────────────────────────");
        Console.WriteLine("  [0]  Exit");
        Console.WriteLine();
        Console.Write("  Choice: ");
    }

    private static void PrintStatus(bool? ok, string text)
    {
        Console.ForegroundColor = ok switch
        {
            true  => ConsoleColor.Green,
            false => ConsoleColor.Red,
            null  => ConsoleColor.DarkGray,
        };
        Console.WriteLine(text);
        Console.ResetColor();
    }

    // ── Option runner ──────────────────────────────────────────────────────────

    private static async Task RunOptionAsync(char key, string mysqlConn, QuickBooksConfig qbConfig)
    {
        Console.WriteLine();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (key == '5')
            {
                Ui.Header("  ── Batch Sync Bills to QuickBooks ───────────────────────");
                using var session = new QbSession(qbConfig);
                Console.WriteLine("  Opening QuickBooks session...");
                session.Open();
                Ui.Ok($"  Session opened.  (QBXML {session.QbXmlVersion})");

                var push = new BillPush(mysqlConn, session);
                await push.RunAsync();
            }
            else
            {
                string label = key switch
                {
                    '1' => "Vendor",
                    '2' => "Item Inventory",
                    '3' => "Inventory Site",
                    _   => "Item Sites (Quantity)",
                };
                Ui.Header($"  ── Pull {label} from QuickBooks ──────────────────────────");

                using var session = new QbSession(qbConfig);
                Console.WriteLine("  Opening QuickBooks session...");
                session.Open();
                Ui.Ok($"  Session opened.  (QBXML {session.QbXmlVersion})");

                SyncResult result = key switch
                {
                    '1' => await new VendorSync(mysqlConn, session).RunAsync(),
                    '2' => await new ItemSync(mysqlConn, session).RunAsync(),
                    '3' => await new InventorySiteSync(mysqlConn, session).RunAsync(),
                    _   => await new ItemSiteSync(mysqlConn, session).RunAsync(),
                };

                sw.Stop();
                Console.WriteLine();
                Ui.Ok($"  Done in {sw.Elapsed.TotalSeconds:F2}s  —  {result}");
            }
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            Console.WriteLine();
            Ui.Error($"  Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine();
            Ui.Error($"  Unexpected error: {ex.Message}");
            Ui.Error($"  {ex.StackTrace}");
        }
    }
}

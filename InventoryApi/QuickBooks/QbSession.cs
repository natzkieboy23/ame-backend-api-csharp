using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace InventoryApi.QuickBooks;

/// <summary>
/// Wraps a single QuickBooks IPC session via QBXMLRP2 (raw XML request processor).
///
/// HOW IT WORKS
/// ────────────
/// QBXMLRP2.RequestProcessor is a COM object registered by QuickBooks Desktop
/// itself (not the SDK installer).  It accepts raw QBXML strings and returns
/// raw QBXML response strings — no object model required.
///
/// The session flow is:
///   OpenConnection2 → BeginSession (returns ticket) → HostQueryRq (version) → ProcessRequest* → EndSession → CloseConnection
///
/// FIRST-RUN AUTHORIZATION
/// ───────────────────────
/// The first time this app calls BeginSession(), QuickBooks will display a dialog
/// asking the user to grant access.  Choose "Yes, always allow access even if QB
/// is not running" and click Continue.  This is a one-time step per company file.
///
/// USAGE
/// ─────
///   using var session = new QbSession(config.QuickBooks);
///   session.Open();
///   string responseXml = session.DoRequests(requestXml);
///   // session.Dispose() is called automatically at end of using block
/// </summary>
public sealed class QbSession : IDisposable
{
    private readonly string _appName;
    private readonly string _companyFile;
    private readonly string _configVersion;   // fallback from appsettings.json

    private dynamic? _manager;
    private string?  _ticket;
    private string?  _negotiatedVersion;       // highest version QB actually supports
    private bool     _disposed;

    /// <summary>
    /// The highest QBXML version supported by this QB installation, resolved after Open().
    /// Falls back to the value in appsettings.json if negotiation fails.
    /// </summary>
    public string QbXmlVersion => _negotiatedVersion ?? _configVersion;

    public QbSession(QuickBooksConfig cfg)
    {
        _appName       = cfg.AppName;
        _companyFile   = cfg.CompanyFile;
        _configVersion = cfg.QbXmlVersion;
    }

    /// <summary>Opens the connection, begins a QB session, and negotiates the QBXML version.</summary>
    public void Open()
    {
        var type = Type.GetTypeFromProgID("QBXMLRP2.RequestProcessor");
        if (type is null)
            throw new InvalidOperationException(
                "QuickBooks XML Request Processor (QBXMLRP2) is not registered.\n" +
                "Make sure QuickBooks Desktop is installed on this machine.");

        _manager = Activator.CreateInstance(type)!;
        _manager.OpenConnection("", _appName);

        // QBOpenModeEnum: 2 = omDontCare (single- or multi-user, QB decides)
        _ticket = _manager.BeginSession(_companyFile, 2);

        // Query QB for its supported QBXML version range and pick the highest.
        _negotiatedVersion = NegotiateVersion();
    }

    /// <summary>
    /// Sends a QBXML request string to QuickBooks and returns the response XML.
    /// </summary>
    public string DoRequests(string xmlRequest)
    {
        if (_manager is null || _ticket is null)
            throw new InvalidOperationException("Session is not open. Call Open() first.");

        return _manager.ProcessRequest(_ticket, xmlRequest);
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            if (_manager is not null)
            {
                if (_ticket is not null)
                {
                    _manager.EndSession(_ticket);
                    _ticket = null;
                }
                _manager.CloseConnection();
                Marshal.ReleaseComObject(_manager);
                _manager = null;
            }
        }
        catch { /* best-effort cleanup — never throw in Dispose */ }
        finally { _disposed = true; }
    }

    // ── Version negotiation ───────────────────────────────────────────────────

    /// <summary>
    /// Sends a HostQueryRq to QB (always valid at version 1.0) and parses the list of
    /// SupportedQBXMLVersion elements to find the highest version the installation supports.
    /// Returns null if the query fails — callers fall back to _configVersion.
    /// </summary>
    private string? NegotiateVersion()
    {
        try
        {
            // HostQueryRq is valid at every QBXML version — use the config version for the PI
            string hostQuery = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <?qbxml version="{_configVersion}"?>
                <QBXML>
                  <QBXMLMsgsRq onError="stopOnError">
                    <HostQueryRq requestID="0"/>
                  </QBXMLMsgsRq>
                </QBXML>
                """;

            string response = _manager!.ProcessRequest(_ticket, hostQuery);
            var doc = XDocument.Parse(response);

            var versions = doc.Descendants("SupportedQBXMLVersion")
                .Select(e => e.Value.Trim())
                .Where(v => Version.TryParse(v, out _))
                .OrderByDescending(v => Version.Parse(v))
                .ToList();

            return versions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Version negotiation failed: {ex.Message}");
            Console.ResetColor();
            return null;   // fall back to appsettings value
        }
    }
}

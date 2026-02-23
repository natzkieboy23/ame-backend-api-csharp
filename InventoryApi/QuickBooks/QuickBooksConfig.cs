namespace InventoryApi.QuickBooks;

public class QuickBooksConfig
{
    /// <summary>Name shown to the QuickBooks authorization dialog on first run.</summary>
    public string AppName { get; set; } = "AME Inventory";

    /// <summary>
    /// Full path to the .qbw company file, e.g. C:\Users\You\Company.qbw
    /// Leave empty to use whatever company file is currently open in QB.
    /// </summary>
    public string CompanyFile { get; set; } = string.Empty;

    /// <summary>QBXML protocol version matching the installed QBFC SDK.</summary>
    public string QbXmlVersion { get; set; } = "10.0";
}

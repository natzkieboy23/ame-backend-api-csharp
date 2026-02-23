namespace InventoryApi.QuickBooks;

public class SyncResult
{
    public int Total    { get; init; }
    public int Inserted { get; init; }
    public int Updated  { get; init; }

    public override string ToString()
        => $"Total: {Total}  |  Inserted: {Inserted}  |  Updated: {Updated}";
}

/// <summary>Console colour helpers shared across sync services.</summary>
public static class Ui
{
    public static void Ok(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void Warn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void Header(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
}

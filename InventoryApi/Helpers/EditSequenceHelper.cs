namespace InventoryApi.Helpers;

public static class EditSequenceHelper
{
    public static string Generate()
        => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public static string Increment(string? current)
    {
        if (long.TryParse(current, out long value))
            return (value + 1).ToString();

        return Generate();
    }
}

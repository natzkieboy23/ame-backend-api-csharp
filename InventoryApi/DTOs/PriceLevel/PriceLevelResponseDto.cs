namespace InventoryApi.DTOs.PriceLevel;

public class PriceLevelResponseDto
{
    public string    ListID                    { get; set; } = string.Empty;
    public DateTime? TimeCreated               { get; set; }
    public DateTime? TimeModified              { get; set; }
    public string?   EditSequence              { get; set; }
    public string?   Name                      { get; set; }
    public string?   IsActive                  { get; set; }
    public string?   PriceLevelType            { get; set; }
    public string?   PriceLevelFixedPercentage { get; set; }
    public string?   CurrencyRef_ListID        { get; set; }
    public string?   CurrencyRef_FullName      { get; set; }
    public string?   UserData                  { get; set; }
    public string?   Status                    { get; set; }

    public IEnumerable<PriceLevelPerItemResponseDto> PerItemDetails { get; set; } = [];
}

public class PriceLevelPerItemResponseDto
{
    public string?   ItemRef_ListID     { get; set; }
    public string?   ItemRef_FullName   { get; set; }
    public decimal?  CustomPrice        { get; set; }
    public string?   CustomPricePercent { get; set; }
    public string?   AdjustPercentage   { get; set; }
    public string?   AdjustRelativeTo   { get; set; }
}

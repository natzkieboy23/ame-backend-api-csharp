namespace InventoryApi.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int? TotalCount { get; set; }

    public static ApiResponse<T> Ok(T? data, string? message = null, int? totalCount = null)
        => new() { Success = true, Message = message, Data = data, TotalCount = totalCount };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}

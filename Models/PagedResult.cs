namespace dot_net_core_rest_api.Models;

public class PagedResult<T>
{
    public required List<T> Items { get; init; }
    public int Total { get; init; }
    public string? Cursor { get; init; }
    public bool HasMore { get; init; }
}

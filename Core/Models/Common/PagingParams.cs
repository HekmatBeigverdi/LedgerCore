namespace LedgerCore.Core.Models.Common;

public class PagingParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // optional global search term
    public string? Search { get; set; }

    // optional order by e.g. "Name desc" or "Code"
    public string? OrderBy { get; set; }
}
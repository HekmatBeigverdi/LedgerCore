namespace LedgerCore.Core.Models.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => 
        (int)Math.Ceiling((double)TotalCount / PageSize);
}
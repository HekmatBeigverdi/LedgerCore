
using Common = LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace LedgerCore.Persistence.Repository;
public static class QueryHelpers
{
    public static async Task<Common.PagedResult<T>> ApplyPagingAsync<T>(
        IQueryable<T> query,
        Common.PagingParams? paging,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (paging == null)
        {
            var all = await query.ToListAsync(cancellationToken);
            return new Common.PagedResult<T>(all, all.Count, 1, all.Count == 0 ? 1 : all.Count);
        }

        if (!string.IsNullOrWhiteSpace(paging.OrderBy))
        {
            try { query = query.OrderBy(paging.OrderBy); }
            catch { /* ignore invalid order */ }
        }

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, paging.PageNumber);
        var size = Math.Clamp(paging.PageSize, 1, 1000);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new Common.PagedResult<T>(items, total, page, size);
    }
}

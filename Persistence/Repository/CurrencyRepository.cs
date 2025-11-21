using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class CurrencyRepository(LedgerCoreDbContext context) : RepositoryBase<Currency>(context), ICurrencyRepository
{
    public Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<PagedResult<Currency>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Currency, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Currency> query = DbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
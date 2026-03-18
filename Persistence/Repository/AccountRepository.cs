using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class AccountRepository(LedgerCoreDbContext context) : RepositoryBase<Account>(context), IAccountRepository
{
    public Task<Account?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<PagedResult<Account>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Account, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = DbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
using System.Linq.Expressions;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class PartyRepository(LedgerCoreDbContext context) : RepositoryBase<Party>(context), IPartyRepository
{
    public Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<PagedResult<Party>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Party, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Party> query = DbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
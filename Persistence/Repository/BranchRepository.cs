using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class BranchRepository(LedgerCoreDbContext context) : RepositoryBase<Branch>(context), IBranchRepository
{
    public async Task<PagedResult<Branch>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Branch> query = DbSet.AsNoTracking();
        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
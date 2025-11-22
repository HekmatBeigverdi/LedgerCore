using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class RoleRepository(LedgerCoreDbContext context) : RepositoryBase<Role>(context), IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<PagedResult<Role>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Role> query = DbSet.AsNoTracking();
        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
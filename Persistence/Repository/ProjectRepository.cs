using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ProjectRepository(LedgerCoreDbContext context) : RepositoryBase<Project>(context), IProjectRepository
{
    public async Task<PagedResult<Project>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = DbSet.AsNoTracking();
        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
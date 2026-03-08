using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class WarehouseRepository(LedgerCoreDbContext context) : RepositoryBase<Warehouse>(context), IWarehouseRepository
{
    public async Task<PagedResult<Warehouse>> QueryAsync(
        int branchId,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Warehouse> query = DbSet
            .Where(x => x.BranchId == branchId)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}

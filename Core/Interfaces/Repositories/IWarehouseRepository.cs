using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Inventory;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<PagedResult<Warehouse>> QueryAsync(
        int branchId,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
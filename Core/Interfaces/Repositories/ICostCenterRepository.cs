using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface ICostCenterRepository : IRepository<CostCenter>
{
    Task<PagedResult<CostCenter>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
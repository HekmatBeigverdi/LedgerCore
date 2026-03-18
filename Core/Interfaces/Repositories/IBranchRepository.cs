using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

/// <summary>
/// Branch repository (simple CRUD + query).
/// </summary>
public interface IBranchRepository : IRepository<Branch>
{
    Task<PagedResult<Branch>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Security;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<PagedResult<Role>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
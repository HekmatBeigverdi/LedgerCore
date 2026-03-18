using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<PagedResult<Project>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Security;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<PagedResult<User>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
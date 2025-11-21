using System.Linq.Expressions;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PagedResult<Account>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Account, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
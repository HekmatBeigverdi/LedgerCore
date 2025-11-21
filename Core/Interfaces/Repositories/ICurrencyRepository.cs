using System.Linq.Expressions;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PagedResult<Currency>> QueryAsync(PagingParams? paging = null,
        Expression<Func<Currency, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
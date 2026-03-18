using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface ITaxRateRepository : IRepository<TaxRate>
{
    Task<PagedResult<TaxRate>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
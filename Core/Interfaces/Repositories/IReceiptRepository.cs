using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IReceiptRepository : IRepository<Receipt>
{
    Task<PagedResult<Receipt>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
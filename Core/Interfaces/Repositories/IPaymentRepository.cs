using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<PagedResult<Payment>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
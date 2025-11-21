using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IChequeRepository : IRepository<Cheque>
{
    Task<IReadOnlyList<Cheque>> GetByStatusAsync(
        ChequeStatus status,
        CancellationToken cancellationToken = default);

    Task AddHistoryAsync(ChequeHistory history, CancellationToken cancellationToken = default);

    Task<PagedResult<Cheque>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
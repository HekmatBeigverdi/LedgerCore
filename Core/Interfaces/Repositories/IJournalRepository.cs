using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IJournalRepository : IRepository<JournalVoucher>
{
    Task<JournalVoucher?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<JournalVoucher>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
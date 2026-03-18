using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IJournalRepository : IRepository<JournalVoucher>
{
    Task<JournalVoucher?> GetWithLinesAsync(int branchId, int id, CancellationToken cancellationToken = default);

    Task<PagedResult<JournalVoucher>> QueryAsync(int branchId,PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
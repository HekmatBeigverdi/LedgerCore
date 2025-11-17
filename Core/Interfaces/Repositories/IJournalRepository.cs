using LedgerCore.Core.Models.Accounting;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IJournalRepository: IRepository<JournalVoucher>
{
    Task<JournalVoucher?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default);

}
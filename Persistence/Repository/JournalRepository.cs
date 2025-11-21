using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class JournalRepository(LedgerCoreDbContext context)
    : RepositoryBase<JournalVoucher>(context), IJournalRepository
{
    public Task<JournalVoucher?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
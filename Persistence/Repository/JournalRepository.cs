using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
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

    public async Task<PagedResult<JournalVoucher>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<JournalVoucher> query = DbSet.AsNoTracking();
        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
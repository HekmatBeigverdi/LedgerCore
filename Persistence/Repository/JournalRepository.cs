using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class JournalRepository(LedgerCoreDbContext context)
    : RepositoryBase<JournalVoucher>(context), IJournalRepository
{
    public Task<JournalVoucher?> GetWithLinesAsync(
        int id,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }

    public async Task<PagedResult<JournalVoucher>> QueryAsync(
        int branchId,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<JournalVoucher> query = DbSet
            .Where(x => x.BranchId == branchId)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
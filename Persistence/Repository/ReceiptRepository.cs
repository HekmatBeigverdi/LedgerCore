using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ReceiptRepository(LedgerCoreDbContext context) : RepositoryBase<Receipt>(context), IReceiptRepository
{
    public async Task<PagedResult<Receipt>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Receipt> query = DbSet
            .Include(x => x.Party)
            .Include(x => x.BankAccount)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
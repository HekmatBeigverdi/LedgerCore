using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class PaymentRepository(LedgerCoreDbContext context) : RepositoryBase<Payment>(context), IPaymentRepository
{
    public async Task<PagedResult<Payment>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Payment> query = DbSet
            .Include(x => x.Party)
            .Include(x => x.BankAccount)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
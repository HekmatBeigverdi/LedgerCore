using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class PaymentRepository(LedgerCoreDbContext context) : RepositoryBase<Payment>(context), IPaymentRepository
{
    public async Task<PagedResult<Payment>> QueryAsync(
        int branchId,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Payment> query = DbSet
            .Include(x => x.Party)
            .Include(x => x.BankAccount)
            .Include(x => x.JournalVoucher)
            .Include(x => x.ReversalJournalVoucher)
            .Where(x => x.BranchId == branchId)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
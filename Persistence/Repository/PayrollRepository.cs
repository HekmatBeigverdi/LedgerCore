using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class PayrollRepository(LedgerCoreDbContext context)
    : RepositoryBase<PayrollDocument>(context), IPayrollRepository
{
    private readonly LedgerCoreDbContext _context = context;

    public Task<PayrollDocument?> GetWithLinesAsync(
        int id,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return _context.PayrollDocuments
            .Include(x => x.Lines)
            .Include(x => x.PayrollPeriod)
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }

    public async Task<PagedResult<PayrollDocument>> QueryAsync(
        int branchId,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PayrollDocument> query = DbSet
            .Include(x => x.PayrollPeriod)
            .Where(x => x.BranchId == branchId)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class PayrollRepository(LedgerCoreDbContext context)
    : RepositoryBase<PayrollDocument>(context), IPayrollRepository
{
    private readonly LedgerCoreDbContext _context = context;

    public Task<PayrollDocument?> GetWithLinesAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.PayrollDocuments
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
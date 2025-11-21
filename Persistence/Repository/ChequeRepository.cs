using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ChequeRepository : RepositoryBase<Cheque>, IChequeRepository
{
    private readonly LedgerCoreDbContext _context;

    public ChequeRepository(LedgerCoreDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<Cheque>> GetByStatusAsync(
        ChequeStatus status,
        CancellationToken cancellationToken = default)
    {
        return _context.Cheques
            .Where(x => x.Status == status)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<Cheque>>(t => t.Result, cancellationToken);
    }

    public async Task AddHistoryAsync(ChequeHistory history, CancellationToken cancellationToken = default)
    {
        await _context.ChequeHistories.AddAsync(history, cancellationToken);
    }
}
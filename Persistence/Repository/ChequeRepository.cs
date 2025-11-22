using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class ChequeRepository(LedgerCoreDbContext context) : RepositoryBase<Cheque>(context), IChequeRepository
{
    private readonly LedgerCoreDbContext _context = context;

    public async Task<IReadOnlyList<Cheque>> GetByStatusAsync(
        ChequeStatus status,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.Cheques
            .Where(x => x.Status == status)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task AddHistoryAsync(ChequeHistory history, CancellationToken cancellationToken = default)
    {
        await _context.ChequeHistories.AddAsync(history, cancellationToken);
    }

    public async Task<PagedResult<Cheque>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Cheque> query = _context.Cheques
            .Include(x => x.Party)
            .Include(x => x.BankAccount)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
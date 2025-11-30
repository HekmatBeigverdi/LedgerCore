using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class FixedAssetRepository(LedgerCoreDbContext context)
    : RepositoryBase<FixedAsset>(context), IFixedAssetRepository
{
    private readonly LedgerCoreDbContext _context = context;

    public async Task<IReadOnlyList<DepreciationSchedule>> GetSchedulesAsync(
        int fixedAssetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DepreciationSchedules
            .Where(x => x.FixedAssetId == fixedAssetId)
            .OrderBy(x => x.PeriodStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddScheduleAsync(
        DepreciationSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        await _context.DepreciationSchedules.AddAsync(schedule, cancellationToken);
    }

    public async Task AddTransactionAsync(
        AssetTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _context.AssetTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<PagedResult<FixedAsset>> QueryAsync(
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<FixedAsset> query = DbSet
            .Include(x => x.Category)
            .Include(x => x.Branch)
            .Include(x => x.CostCenter)
            .Include(x => x.Project)
            .AsNoTracking();

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
    }
}
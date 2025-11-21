using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Assets;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class FixedAssetRepository(LedgerCoreDbContext context)
    : RepositoryBase<FixedAsset>(context), IFixedAssetRepository
{
    private readonly LedgerCoreDbContext _context = context;

    public Task<IReadOnlyList<DepreciationSchedule>> GetSchedulesAsync(
        int fixedAssetId,
        CancellationToken cancellationToken = default)
    {
        return _context.DepreciationSchedules
            .Where(x => x.FixedAssetId == fixedAssetId)
            .OrderBy(x => x.PeriodStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<DepreciationSchedule>>(t => t.Result, cancellationToken);
    }

    public async Task AddScheduleAsync(DepreciationSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.DepreciationSchedules.AddAsync(schedule, cancellationToken);
    }

    public async Task AddTransactionAsync(AssetTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.AssetTransactions.AddAsync(transaction, cancellationToken);
    }
}
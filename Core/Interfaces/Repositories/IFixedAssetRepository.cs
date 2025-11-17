using LedgerCore.Core.Models.Assets;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IFixedAssetRepository: IRepository<FixedAsset>
{
    Task<IReadOnlyList<DepreciationSchedule>> GetSchedulesAsync(
        int fixedAssetId,
        CancellationToken cancellationToken = default);

    Task AddScheduleAsync(DepreciationSchedule schedule, CancellationToken cancellationToken = default);

    Task AddTransactionAsync(AssetTransaction transaction, CancellationToken cancellationToken = default);
}
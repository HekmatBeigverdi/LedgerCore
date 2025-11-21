using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IFixedAssetRepository : IRepository<FixedAsset>
{
    Task<IReadOnlyList<DepreciationSchedule>> GetSchedulesAsync(int fixedAssetId, CancellationToken cancellationToken = default);
    Task AddScheduleAsync(DepreciationSchedule schedule, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(AssetTransaction transaction, CancellationToken cancellationToken = default);

    Task<PagedResult<FixedAsset>> QueryAsync(PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
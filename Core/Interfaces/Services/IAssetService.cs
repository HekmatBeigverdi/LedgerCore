using LedgerCore.Core.Models.Assets;

namespace LedgerCore.Core.Interfaces.Services;

public interface IAssetService
{
    Task<FixedAsset> CreateFixedAssetAsync(
        FixedAsset asset,
        CancellationToken cancellationToken = default);

    Task GenerateDepreciationScheduleAsync(
        int fixedAssetId,
        CancellationToken cancellationToken = default);

    Task PostDepreciationForPeriodAsync(
        int fixedAssetId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);
}
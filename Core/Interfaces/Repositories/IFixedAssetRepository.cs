using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IFixedAssetRepository : IRepository<FixedAsset>
{
    /// <summary>
    /// تمام برنامه‌های استهلاک ثبت‌شده برای یک دارایی.
    /// </summary>
    Task<IReadOnlyList<DepreciationSchedule>> GetSchedulesAsync(
        int fixedAssetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// افزودن یک رکورد برنامه استهلاک.
    /// </summary>
    Task AddScheduleAsync(
        DepreciationSchedule schedule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ثبت تراکنش روی دارایی (استهلاک، فروش، بهبود، ...)
    /// </summary>
    Task AddTransactionAsync(
        AssetTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// جستجوی صفحه‌بندی‌شده‌ی دارایی‌ها (برای Grid فرانت‌اند).
    /// </summary>
    Task<PagedResult<FixedAsset>> QueryAsync(
        PagingParams? paging = null,
        CancellationToken cancellationToken = default);
}
using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Services;

public class AssetService(
    IUnitOfWork uow,
    IFixedAssetRepository fixedAssets,
    ICurrentBranchService currentBranch,
    INumberSeriesService numberSeries,
    IPostingEngineService postingEngine)
    : IAssetService
{
    private readonly ICurrentBranchService _currentBranch = currentBranch;
    
    /// Helper Methods Start
    private int GetBranchIdOrThrow()
        => currentBranch.GetRequiredBranchId();

    private async Task<FixedAsset?> GetFixedAssetScopedAsync(int id, CancellationToken ct)
    {
        var branchId = GetBranchIdOrThrow();
        var page = await uow.Repository<FixedAsset>().FindAsync(
            x => x.Id == id && x.BranchId == branchId,
            null,
            ct);

        return page.Items.FirstOrDefault();
    }

    private async Task<FixedAsset> GetFixedAssetScopedOrThrowAsync(int id, CancellationToken ct)
    {
        var asset = await GetFixedAssetScopedAsync(id, ct);
        if (asset is null)
            throw new InvalidOperationException($"FixedAsset with id={id} not found.");

        return asset;
    }
    /// Helper Methods End

    
    
    /// <summary>
    /// ایجاد دارایی ثابت جدید.
    /// اگر UsefulLifeMonths صفر باشد، مقدار دسته را وارد می‌کند.
    /// Status را Active می‌کند و استهلاک انباشته را صفر.
    /// </summary>
    public async Task<FixedAsset> CreateFixedAssetAsync(
        FixedAsset asset,
        CancellationToken cancellationToken = default)
    {
        
        var currentBranchId = GetBranchIdOrThrow();
        
        if (asset.BranchId == 0)
            asset.BranchId = currentBranchId;
        else if (asset.BranchId != currentBranchId)
            throw new InvalidOperationException("BranchId is not valid for current branch scope.");
        
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // خواندن دسته برای تنظیم عمر مفید و مقدار اسقاط در صورت نیاز
            var categoryRepo = uow.Repository<AssetCategory>();
            var category = await categoryRepo.GetByIdAsync(asset.CategoryId, cancellationToken)
                          ?? throw new InvalidOperationException($"AssetCategory with id={asset.CategoryId} not found.");

            if (asset.UsefulLifeMonths <= 0)
                asset.UsefulLifeMonths = category.DefaultUsefulLifeMonths;

            if (asset.ResidualValue < 0)
                throw new InvalidOperationException("ResidualValue cannot be negative.");

            asset.Status = AssetStatus.Active;
            asset.AccumulatedDepreciation = 0m;

            await fixedAssets.AddAsync(asset, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
            return asset;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// تولید برنامه استهلاک برای کل عمر دارایی.
    /// اگر قبلاً برنامه‌ای وجود داشته باشد، فعلاً خطا می‌دهیم تا از دوباره‌کاری جلوگیری شود.
    /// </summary>
    public async Task GenerateDepreciationScheduleAsync(
        int fixedAssetId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var asset = await GetFixedAssetScopedOrThrowAsync(fixedAssetId, cancellationToken);

            var branchId = GetBranchIdOrThrow();
            var existingSchedules = await fixedAssets.GetSchedulesAsync(
                fixedAssetId,
                branchId,
                cancellationToken);

            if (existingSchedules.Any())
                throw new InvalidOperationException("Depreciation schedule already exists for this asset.");

            var categoryRepo = uow.Repository<AssetCategory>();
            var category = await categoryRepo.GetByIdAsync(asset.CategoryId, cancellationToken)
                          ?? throw new InvalidOperationException($"AssetCategory with id={asset.CategoryId} not found.");

            var usefulLife = asset.UsefulLifeMonths > 0
                ? asset.UsefulLifeMonths
                : category.DefaultUsefulLifeMonths;

            if (usefulLife <= 0)
                throw new InvalidOperationException("Useful life months must be greater than zero.");

            // محاسبه ارزش اسقاط
            var residual = asset.ResidualValue;
            if (residual == 0 && category.DefaultResidualPercent > 0)
            {
                residual = asset.AcquisitionCost * category.DefaultResidualPercent / 100m;
            }

            if (residual < 0 || residual >= asset.AcquisitionCost)
                throw new InvalidOperationException("Residual value must be between 0 and acquisition cost.");

            var depreciableBase = asset.AcquisitionCost - residual;
            var monthly = decimal.Round(depreciableBase / usefulLife, 2);

            // تولید ماه به ماه
            var schedules = new List<DepreciationSchedule>();
            decimal accumulated = 0m;

            // شروع از اول ماه تاریخ تحصیل
            var start = new DateTime(asset.AcquisitionDate.Year, asset.AcquisitionDate.Month, 1);

            for (var i = 0; i < usefulLife; i++)
            {
                var periodStart = start.AddMonths(i);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                var amount = monthly;

                // آخرین ماه: تنظیم مقدار استهلاک تا NetBookValue دقیقاً residual شود
                if (i == usefulLife - 1)
                {
                    amount = depreciableBase - accumulated;
                }

                accumulated += amount;
                var nbv = asset.AcquisitionCost - accumulated;

                var schedule = new DepreciationSchedule
                {
                    FixedAssetId = asset.Id,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    DepreciationAmount = amount,
                    AccumulatedDepreciation = accumulated,
                    NetBookValue = nbv,
                    IsPosted = false
                };

                schedules.Add(schedule);
            }

            foreach (var s in schedules)
            {
                await fixedAssets.AddScheduleAsync(s, cancellationToken);
            }

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// ثبت استهلاک برای یک بازه مشخص (یک رکورد از DepreciationSchedule)
    /// و ایجاد سند حسابداری (هزینه استهلاک ↔ استهلاک انباشته).
    /// DocumentType برای PostingRule = "AssetDepreciation"
    /// </summary>
    public async Task PostDepreciationForPeriodAsync(
        int fixedAssetId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var asset = await GetFixedAssetScopedOrThrowAsync(fixedAssetId, cancellationToken);

            var branchId = GetBranchIdOrThrow();
            var schedules = await fixedAssets.GetSchedulesAsync(
                fixedAssetId,
                branchId,
                cancellationToken);

            var schedule = schedules.FirstOrDefault(x =>
                x.PeriodStart.Date == periodStart.Date &&
                x.PeriodEnd.Date == periodEnd.Date);

            if (schedule is null)
                throw new InvalidOperationException("Depreciation schedule not found for the given period.");

            if (schedule.IsPosted)
                return; // قبلاً ثبت شده
            
            var context = new PostingContext
            {
                Total = schedule.DepreciationAmount,
                Description = $"Depreciation for asset {asset.Code} - {periodStart:yyyy/MM/dd} to {periodEnd:yyyy/MM/dd}"
            };

            var journal = await postingEngine.BuildJournalAsync(
                documentType: "AssetDepreciation",
                branchId: asset.BranchId,
                date: schedule.PeriodEnd,
                refDocumentId: schedule.Id,
                refDocumentNumber: asset.Code,
                context: context,
                cancellationToken: cancellationToken);
            
            await uow.Journals.AddAsync(journal, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            schedule.IsPosted = true;
            schedule.JournalVoucher = journal;

            asset.AccumulatedDepreciation += schedule.DepreciationAmount;

            if (schedule.NetBookValue <= asset.ResidualValue + 1m)
            {
                asset.Status = AssetStatus.FullyDepreciated;
            }

            fixedAssets.Update(asset);
            await uow.SaveChangesAsync(cancellationToken);


            var transaction = new AssetTransaction
            {
                FixedAssetId = asset.Id,
                TransactionType = AssetTransactionType.Depreciation,
                TransactionDate = schedule.PeriodEnd,
                Amount = schedule.DepreciationAmount,
                Description = $"Depreciation posted for period {periodStart:yyyy/MM/dd} - {periodEnd:yyyy/MM/dd}",
                JournalVoucher = journal
            };

            await fixedAssets.AddTransactionAsync(transaction, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
}
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _uow;
    private readonly IFixedAssetRepository _fixedAssets;

    public AssetService(IUnitOfWork uow, IFixedAssetRepository fixedAssets)
    {
        _uow = uow;
        _fixedAssets = fixedAssets;
    }

    /// <summary>
    /// ایجاد دارایی ثابت جدید.
    /// اگر UsefulLifeMonths صفر باشد، مقدار دسته را وارد می‌کند.
    /// Status را Active می‌کند و استهلاک انباشته را صفر.
    /// </summary>
    public async Task<FixedAsset> CreateFixedAssetAsync(
        FixedAsset asset,
        CancellationToken cancellationToken = default)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // خواندن دسته برای تنظیم عمر مفید و مقدار اسقاط در صورت نیاز
            var categoryRepo = _uow.Repository<AssetCategory>();
            var category = await categoryRepo.GetByIdAsync(asset.CategoryId, cancellationToken)
                          ?? throw new InvalidOperationException($"AssetCategory with id={asset.CategoryId} not found.");

            if (asset.UsefulLifeMonths <= 0)
                asset.UsefulLifeMonths = category.DefaultUsefulLifeMonths;

            if (asset.ResidualValue < 0)
                throw new InvalidOperationException("ResidualValue cannot be negative.");

            asset.Status = AssetStatus.Active;
            asset.AccumulatedDepreciation = 0m;

            await _fixedAssets.AddAsync(asset, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            await _uow.CommitTransactionAsync(cancellationToken);
            return asset;
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
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
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var asset = await _fixedAssets.GetByIdAsync(fixedAssetId, cancellationToken)
                        ?? throw new InvalidOperationException($"FixedAsset with id={fixedAssetId} not found.");

            var existingSchedules = await _fixedAssets.GetSchedulesAsync(fixedAssetId, cancellationToken);
            if (existingSchedules.Any())
                throw new InvalidOperationException("Depreciation schedule already exists for this asset.");

            var categoryRepo = _uow.Repository<AssetCategory>();
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
                await _fixedAssets.AddScheduleAsync(s, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
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
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var asset = await _fixedAssets.GetByIdAsync(fixedAssetId, cancellationToken)
                        ?? throw new InvalidOperationException($"FixedAsset with id={fixedAssetId} not found.");

            var schedules = await _fixedAssets.GetSchedulesAsync(fixedAssetId, cancellationToken);
            var schedule = schedules.FirstOrDefault(x =>
                x.PeriodStart.Date == periodStart.Date &&
                x.PeriodEnd.Date == periodEnd.Date);

            if (schedule is null)
                throw new InvalidOperationException("Depreciation schedule not found for the given period.");

            if (schedule.IsPosted)
                return; // قبلاً ثبت شده

            // خواندن PostingRule
            var postingRuleRepo = _uow.Repository<PostingRule>();
            var rulePage = await postingRuleRepo.FindAsync(
                x => x.DocumentType == "AssetDepreciation" && x.IsActive,
                null,
                cancellationToken);

            var rule = rulePage.Items.FirstOrDefault()
                       ?? throw new InvalidOperationException("No posting rule defined for AssetDepreciation.");

            // ساخت سند حسابداری
            var journal = new JournalVoucher
            {
                Number = await GenerateNextNumberAsync("Journal", asset.BranchId, cancellationToken),
                Date = schedule.PeriodEnd,
                BranchId = asset.BranchId,
                Description = $"Depreciation for asset {asset.Code} - {periodStart:yyyy/MM/dd} to {periodEnd:yyyy/MM/dd}",
                Status = DocumentStatus.Posted
            };

            var lines = new List<JournalLine>();
            int lineNo = 1;

            // Debit: هزینه استهلاک
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = rule.DebitAccountId,
                Debit = schedule.DepreciationAmount,
                Credit = 0,
                RefDocumentType = "Depreciation",
                RefDocumentId = schedule.Id,
                Description = $"Depreciation expense for asset {asset.Code}"
            });

            // Credit: استهلاک انباشته
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = rule.CreditAccountId,
                Debit = 0,
                Credit = schedule.DepreciationAmount,
                RefDocumentType = "Depreciation",
                RefDocumentId = schedule.Id,
                Description = $"Accumulated depreciation for asset {asset.Code}"
            });

            journal.Lines = lines;

            await _uow.Journals.AddAsync(journal, cancellationToken);

            // به‌روزرسانی برنامه استهلاک
            schedule.IsPosted = true;
            schedule.JournalVoucher = journal;

            // آپدیت دارایی
            asset.AccumulatedDepreciation += schedule.DepreciationAmount;

            // اگر تقریباً به اسقاط رسید → وضعیت FullyDepreciated
            if (asset.NetBookValue <= asset.ResidualValue + 1) // کمی تلورانس
            {
                asset.Status = AssetStatus.FullyDepreciated;
            }

            _fixedAssets.Update(asset);
            await _uow.SaveChangesAsync(cancellationToken);

            // ثبت تراکنش دارایی
            var transaction = new AssetTransaction
            {
                FixedAssetId = asset.Id,
                TransactionType = AssetTransactionType.Depreciation,
                TransactionDate = schedule.PeriodEnd,
                Amount = schedule.DepreciationAmount,
                Description = $"Depreciation posted for period {periodStart:yyyy/MM/dd} - {periodEnd:yyyy/MM/dd}",
                JournalVoucher = journal
            };

            await _fixedAssets.AddTransactionAsync(transaction, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<string> GenerateNextNumberAsync(
        string entityType,
        int? branchId,
        CancellationToken cancellationToken)
    {
        var seriesRepo = _uow.Repository<NumberSeries>();

        var page = await seriesRepo.FindAsync(
            x => x.EntityType == entityType
                 && x.IsActive
                 && (x.BranchId == null || x.BranchId == branchId),
            null,
            cancellationToken);

        var series = page.Items
            .OrderByDescending(x => x.BranchId.HasValue)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No NumberSeries defined for entityType={entityType}.");

        series.CurrentNumber += 1;
        seriesRepo.Update(series);
        await _uow.SaveChangesAsync(cancellationToken);

        var num = series.CurrentNumber.ToString().PadLeft(series.Padding, '0');
        return $"{series.Prefix}{num}{series.Suffix}";
    }
}
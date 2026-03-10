using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class AssetService(
    IUnitOfWork uow,
    IFixedAssetRepository fixedAssets,
    ICurrentBranchService currentBranch)
    : IAssetService
{
    private readonly ICurrentBranchService _currentBranch = currentBranch;
    
    /// Helper Methods Start
    private int GetBranchIdOrThrow()
        => _currentBranch.GetRequiredBranchId();

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


    private async Task<int> GetOpenFiscalPeriodIdAsync(DateTime date, CancellationToken ct)
    {
        var fyRepo = uow.Repository<FiscalYear>();
        var fyPage = await fyRepo.FindAsync(y => y.StartDate <= date && y.EndDate >= date, null, ct);
        var year = fyPage.Items.OrderByDescending(y => y.StartDate).FirstOrDefault()
                   ?? throw new InvalidOperationException($"No fiscal year found for date={date:yyyy-MM-dd}.");

        if (year.IsClosed)
            throw new InvalidOperationException($"Fiscal year '{year.Name}' is closed.");

        var fpRepo = uow.Repository<FiscalPeriod>();
        var fpPage = await fpRepo.FindAsync(p => p.FiscalYearId == year.Id && p.StartDate <= date && p.EndDate >= date, null, ct);
        var period = fpPage.Items.OrderByDescending(p => p.StartDate).FirstOrDefault()
                     ?? throw new InvalidOperationException($"No fiscal period found for date={date:yyyy-MM-dd}.");

        if (period.IsClosed)
            throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed.");

        return period.Id;
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

            // خواندن PostingRule
            var postingRuleRepo = uow.Repository<PostingRule>();
            var rulePage = await postingRuleRepo.FindAsync(
                x => x.DocumentType == "AssetDepreciation" && x.IsActive,
                null,
                cancellationToken);

            var rule = rulePage.Items.FirstOrDefault()
                       ?? throw new InvalidOperationException("No posting rule defined for AssetDepreciation.");
            
            var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(schedule.PeriodEnd, cancellationToken);


            // ساخت سند حسابداری
            var journal = new JournalVoucher
            {
                Number = await GenerateNextNumberAsync("Journal", asset.BranchId, cancellationToken),
                Date = schedule.PeriodEnd,
                BranchId = asset.BranchId,
                FiscalPeriodId = fiscalPeriodId,
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

            await uow.Journals.AddAsync(journal, cancellationToken);

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

            fixedAssets.Update(asset);
            await uow.SaveChangesAsync(cancellationToken);

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

    private async Task<string> GenerateNextNumberAsync(
        string entityType,
        int? branchId,
        CancellationToken cancellationToken)
    {
        var seriesRepo = uow.Repository<NumberSeries>();

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
        await uow.SaveChangesAsync(cancellationToken);

        var num = series.CurrentNumber.ToString().PadLeft(series.Padding, '0');
        return $"{series.Prefix}{num}{series.Suffix}";
    }
}
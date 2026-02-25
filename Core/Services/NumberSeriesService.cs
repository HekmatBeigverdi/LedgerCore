using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

public class NumberSeriesService(LedgerCoreDbContext db) : INumberSeriesService
{
    private readonly LedgerCoreDbContext _db = db;

    public async Task<string> NextAsync(string seriesCode, int? branchId, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var series = await _db.NumberSeries
                             .FirstOrDefaultAsync(x => x.Code == seriesCode && x.BranchId == branchId, cancellationToken)
                         ?? await _db.NumberSeries.FirstOrDefaultAsync(x => x.Code == seriesCode && x.BranchId == null, cancellationToken);

            if (series is null)
                throw new InvalidOperationException($"NumberSeries '{seriesCode}' not found.");

            series.CurrentNumber += 1;
            series.ModifiedAt = DateTime.UtcNow;

            var number = $"{series.Prefix}{series.CurrentNumber.ToString().PadLeft(series.Padding, '0')}{series.Suffix}";

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return number;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                // یک درخواست دیگر همزمان شماره را تغییر داده؛ مجدد تلاش می‌کنیم
                foreach (var entry in _db.ChangeTracker.Entries())
                    entry.State = EntityState.Detached;
            }
        }

        // اگر بعد از چند تلاش هنوز همزمانی وجود داشت
        throw new InvalidOperationException("Could not generate next number due to concurrent updates. Please retry.");
    }
}
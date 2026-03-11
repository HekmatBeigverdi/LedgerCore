using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

public class NumberSeriesService(LedgerCoreDbContext db) : INumberSeriesService
{
    private readonly LedgerCoreDbContext _db = db;

    public async Task<string> NextAsync(string entityType, int? branchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("entityType is required.", nameof(entityType));

        await using var tx = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var candidates = await _db.NumberSeries
                .AsTracking()
                .Where(x =>
                    x.IsActive &&
                    x.EntityType == entityType &&
                    (x.BranchId == branchId || x.BranchId == null))
                .ToListAsync(cancellationToken);

            var exactBranchSeries = candidates
                .Where(x => x.BranchId == branchId && x.BranchId != null)
                .ToList();

            var globalSeries = candidates
                .Where(x => x.BranchId == null)
                .ToList();

            if (exactBranchSeries.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple active NumberSeries found for entityType='{entityType}' and branchId='{branchId}'.");
            }

            if (globalSeries.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple active global NumberSeries found for entityType='{entityType}'.");
            }

            var series = exactBranchSeries.FirstOrDefault() ?? globalSeries.FirstOrDefault();

            if (series is null)
            {
                throw new InvalidOperationException(
                    $"No active NumberSeries defined for entityType='{entityType}' and branchId='{branchId}'.");
            }

            series.CurrentNumber += 1;
            series.ModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var prefix = series.Prefix ?? string.Empty;
            var suffix = series.Suffix ?? string.Empty;
            var padding = series.Padding < 1 ? 1 : series.Padding;
            var number = series.CurrentNumber.ToString().PadLeft(padding, '0');

            return $"{prefix}{number}{suffix}";
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
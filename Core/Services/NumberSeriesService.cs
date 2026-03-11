using System.Data;
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

        var candidates = await _db.NumberSeries
            .Where(x =>
                x.IsActive &&
                x.EntityType == entityType &&
                (x.BranchId == branchId || x.BranchId == null))
            .ToListAsync(cancellationToken);

        var series = candidates
            .OrderByDescending(x => x.BranchId.HasValue && x.BranchId == branchId)
            .ThenByDescending(x => x.BranchId.HasValue)
            .FirstOrDefault();

        if (series is null)
            throw new InvalidOperationException(
                $"No active NumberSeries defined for entityType='{entityType}' and branchId='{branchId}'.");

        series.CurrentNumber += 1;
        series.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var suffix = series.Suffix ?? string.Empty;
        var number = series.CurrentNumber.ToString().PadLeft(series.Padding, '0');

        return $"{series.Prefix}{number}{suffix}";
    }
}
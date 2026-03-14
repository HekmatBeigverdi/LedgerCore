using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Settings;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LedgerCore.Core.Services;

public class NumberSeriesService(LedgerCoreDbContext db) : INumberSeriesService
{
    private readonly LedgerCoreDbContext _db = db;

    public async Task<string> NextAsync(
        string entityType,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("entityType is required.", nameof(entityType));

        var currentTransaction = _db.Database.CurrentTransaction;
        var ownsTransaction = currentTransaction is null;
        IDbContextTransaction? tx = currentTransaction;

        if (ownsTransaction)
        {
            tx = await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
        }

        try
        {
            var series = await ResolveSeriesAsync(entityType, branchId, cancellationToken);

            if (series is null)
            {
                throw new InvalidOperationException(
                    $"No active NumberSeries defined for code='{entityType}' and branchId='{branchId}'.");
            }

            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE `NumberSeries`
SET
    `CurrentNumber` = `CurrentNumber` + 1,
    `ModifiedAt` = UTC_TIMESTAMP()
WHERE `Id` = {series.Id};", cancellationToken);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    $"Failed to increment NumberSeries for code='{entityType}' and branchId='{branchId}'.");
            }

            var latest = await _db.NumberSeries
                .AsNoTracking()
                .Where(x => x.Id == series.Id)
                .Select(x => new
                {
                    x.CurrentNumber,
                    x.Prefix,
                    x.Suffix,
                    x.Padding
                })
                .SingleAsync(cancellationToken);

            if (ownsTransaction)
            {
                await tx!.CommitAsync(cancellationToken);
            }

            var prefix = latest.Prefix ?? string.Empty;
            var suffix = latest.Suffix ?? string.Empty;
            var padding = latest.Padding < 1 ? 1 : latest.Padding;
            var number = latest.CurrentNumber.ToString().PadLeft(padding, '0');

            return $"{prefix}{number}{suffix}";
        }
        catch
        {
            if (ownsTransaction && tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (ownsTransaction && tx is not null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    private async Task<NumberSeries?> ResolveSeriesAsync(
        string code,
        int? branchId,
        CancellationToken cancellationToken)
    {
        NumberSeries? branchSeries = null;

        if (branchId.HasValue)
        {
            branchSeries = await _db.NumberSeries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.IsActive &&
                         x.Code == code &&
                         x.BranchId == branchId,
                    cancellationToken);
        }

        if (branchSeries is not null)
            return branchSeries;

        var globalSeries = await _db.NumberSeries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IsActive &&
                     x.Code == code &&
                     x.BranchId == null,
                cancellationToken);

        return globalSeries;
    }
}
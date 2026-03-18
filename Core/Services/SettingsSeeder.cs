using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public static class SettingsSeeder
{
    public static async Task SeedAsync(IUnitOfWork uow, CancellationToken cancellationToken = default)
    {
        await SeedNumberSeriesAsync(uow, cancellationToken);
    }

    private static async Task SeedNumberSeriesAsync(IUnitOfWork uow, CancellationToken cancellationToken)
    {
        var seriesRepo = uow.Repository<NumberSeries>();

        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.Journal, NumberSeriesKeys.Journal, "JV-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.SalesInvoice, NumberSeriesKeys.SalesInvoice, "SI-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.PurchaseInvoice, NumberSeriesKeys.PurchaseInvoice, "PI-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.Receipt, NumberSeriesKeys.Receipt, "RC-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.Payment, NumberSeriesKeys.Payment, "PY-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.InventoryAdjustment, NumberSeriesKeys.InventoryAdjustment, "IA-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.CashTransfer, NumberSeriesKeys.CashTransfer, "CT-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.Payroll, NumberSeriesKeys.Payroll, "PR-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.OpeningJournal, NumberSeriesKeys.OpeningJournal, "OPN-", 6, "", cancellationToken);
        await EnsureSeriesAsync(seriesRepo, NumberSeriesKeys.ClosingJournal, NumberSeriesKeys.ClosingJournal, "CLO-", 6, "", cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSeriesAsync(
        IRepository<NumberSeries> repo,
        string entityType,
        string code,
        string prefix,
        int padding,
        string? suffix,
        CancellationToken cancellationToken)
    {
        var page = await repo.FindAsync(
            x => x.EntityType == entityType && x.IsActive && x.BranchId == null,
            pagingParams: null,
            cancellationToken);

        if (page.Items.Any())
            return;

        await repo.AddAsync(new NumberSeries
        {
            EntityType = entityType,
            Code = code,
            BranchId = null,
            Prefix = prefix,
            Padding = padding,
            CurrentNumber = 0,
            Suffix = suffix,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SystemSeeder",
            IsDeleted = false
        }, cancellationToken);
    }
}
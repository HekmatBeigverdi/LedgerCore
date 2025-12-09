using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

/// <summary>
/// سرویس دامین برای مدیریت انتقال وجه (بین حساب‌های بانکی / صندوق‌ها).
/// این سرویس فقط از IUnitOfWork، CashTransfer و NumberSeries استفاده می‌کند.
/// </summary>
public class CashTransferService(IUnitOfWork uow) : ICashTransferService
{
    /// <summary>
    /// ایجاد یک سند انتقال وجه جدید.
    /// </summary>
    public async Task<CashTransfer> CreateCashTransferAsync(
        CashTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // اعتبارسنجی‌های پایه
            if (transfer.Amount <= 0)
                throw new InvalidOperationException("مبلغ انتقال باید بزرگ‌تر از صفر باشد.");

            var hasFrom =
                transfer.FromBankAccountId.HasValue ||
                !string.IsNullOrWhiteSpace(transfer.FromCashDeskCode);

            var hasTo =
                transfer.ToBankAccountId.HasValue ||
                !string.IsNullOrWhiteSpace(transfer.ToCashDeskCode);

            if (!hasFrom || !hasTo)
                throw new InvalidOperationException("مبدأ و مقصد انتقال باید مشخص باشند (حساب بانکی یا صندوق).");

            if (transfer.FromBankAccountId.HasValue &&
                transfer.ToBankAccountId.HasValue &&
                transfer.FromBankAccountId == transfer.ToBankAccountId)
            {
                throw new InvalidOperationException("حساب بانکی مبدأ و مقصد نمی‌توانند یکسان باشند.");
            }

            // اگر شماره خالی است، از NumberSeries بساز
            if (string.IsNullOrWhiteSpace(transfer.Number))
            {
                transfer.Number = await GenerateNextNumberAsync(
                    "CashTransfer",
                    cancellationToken);
            }

            // وضعیت اولیه
            transfer.Status = DocumentStatus.Draft;

            var repo = uow.Repository<CashTransfer>();
            await repo.AddAsync(transfer, cancellationToken);

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);

            return transfer;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// دریافت یک سند انتقال وجه بر اساس Id.
    /// </summary>
    public async Task<CashTransfer?> GetCashTransferAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var repo = uow.Repository<CashTransfer>();
        return await repo.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// ثبت (Post) سند انتقال وجه.
    /// فعلاً فقط وضعیت را Posted می‌کند؛
    /// در گام بعدی می‌توانیم ایجاد JournalVoucher را هم اضافه کنیم.
    /// </summary>
    public async Task PostCashTransferAsync(
        int transferId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var repo = uow.Repository<CashTransfer>();
            var transfer = await repo.GetByIdAsync(transferId, cancellationToken)
                           ?? throw new InvalidOperationException("سند انتقال وجه یافت نشد.");

            if (transfer.Status == DocumentStatus.Posted)
                return;

            if (transfer.Status == DocumentStatus.Cancelled)
                throw new InvalidOperationException("سند لغو شده قابل ثبت نیست.");

            // TODO: در آینده اینجا می‌توانی JournalVoucher برای انتقال وجه بسازی.
            transfer.Status = DocumentStatus.Posted;

            repo.Update(transfer);
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
    /// تولید شماره بعدی برای CashTransfer از جدول NumberSeries.
    /// مشابه متد GenerateNextNumberAsync در AccountingService،
    /// ولی بدون BranchId (چون CashTransfer فعلاً Branch ندارد).
    /// </summary>
    private async Task<string> GenerateNextNumberAsync(
        string entityType,
        CancellationToken cancellationToken)
    {
        var seriesRepo = uow.Repository<NumberSeries>();

        var page = await seriesRepo.FindAsync(
            x => x.EntityType == entityType && x.IsActive,
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
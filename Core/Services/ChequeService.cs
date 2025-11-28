using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Services;

public class ChequeService(IUnitOfWork uow) : IChequeService
{
    public async Task<Cheque> RegisterChequeAsync(Cheque cheque, CancellationToken cancellationToken = default)
    {
        // ✅ تعیین وضعیت اولیه بر اساس IsIncoming
        // چک دریافتی: Received
        // چک صادره: Issued
        
        cheque.Status = cheque.IsIncoming ? ChequeStatus.Received : ChequeStatus.Issued;

        await uow.Cheques.AddAsync(cheque, cancellationToken);

        var history = new ChequeHistory
        {
            Cheque = cheque,
            ChangeDate = DateTime.UtcNow,
            Status = cheque.Status,
            Description = cheque.Description,
            ChangedBy = "system" // بعداً از کاربر لاگین شده بگیر
        };

        await uow.Cheques.AddHistoryAsync(history, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return cheque;
    }

    public async Task ChangeStatusAsync(
        int chequeId,
        ChequeStatus newStatus,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var cheque = await uow.Cheques.GetByIdAsync(chequeId, cancellationToken);
        if (cheque is null)
            throw new InvalidOperationException($"Cheque with id={chequeId} not found.");

        cheque.Status = newStatus;
        uow.Cheques.Update(cheque);

        var history = new ChequeHistory
        {
            ChequeId = cheque.Id,
            ChangeDate = DateTime.UtcNow,
            Status = newStatus,
            Description = comment,
            ChangedBy = "system"
        };

        await uow.Cheques.AddHistoryAsync(history, cancellationToken);

        // اگر بخواهی این‌جا می‌توانی بسته به newStatus سند حسابداری هم بسازی
        // مثلا:
        // - InCollection -> حساب "چک‌های در جریان وصول"
        // - Cleared -> انتقال به "بانک"
        // - Returned/Bounced -> برگشت به حساب مشتری و ثبت جریمه و ...

        await uow.SaveChangesAsync(cancellationToken);
    }
}
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Accounting;

public class PostingRule: AuditableEntity
{
    public string Name { get; set; } = default!;       // مثال: SalesInvoice_Default
    public string DocumentType { get; set; } = default!; // مثلاً "SalesInvoice", "PurchaseInvoice", "Receipt"

    public int DebitAccountId { get; set; }            // حساب طرف بدهکار
    public int CreditAccountId { get; set; }           // حساب طرف بستانکار

    public int? TaxAccountId { get; set; }             // حساب مالیات (در صورت وجود)
    public int? DiscountAccountId { get; set; }        // حساب تخفیف (در صورت نیاز)

    public bool IsActive { get; set; } = true;
}
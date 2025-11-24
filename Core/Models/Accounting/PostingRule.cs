using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Accounting;
/// <summary>
/// Defines how a business document (invoice, receipt, etc.) should be posted to accounts.
/// For example: SalesInvoice → Debit: Receivable, Credit: Sales.
/// </summary>
public class PostingRule: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;          // مثال: SalesInvoice_Default

    // مثلا "SalesInvoice", "PurchaseInvoice", "Receipt"
    public string DocumentType { get; set; } = default!;  

    public int DebitAccountId { get; set; }              // حساب طرف بدهکار
    public int CreditAccountId { get; set; }             // حساب طرف بستانکار

    public int? TaxAccountId { get; set; }               // حساب مالیات (در صورت وجود)
    public int? DiscountAccountId { get; set; }          // حساب تخفیف (در صورت نیاز)

    public bool IsActive { get; set; } = true;
    
    // Navigation properties (اختیاری)
    public Account? DebitAccount { get; set; }
    public Account? CreditAccount { get; set; }
    public Account? TaxAccount { get; set; }
    public Account? DiscountAccount { get; set; }
}
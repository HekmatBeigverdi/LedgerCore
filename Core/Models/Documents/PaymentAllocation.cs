using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Documents;

public class PaymentAllocation : AuditableEntity
{
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public decimal AllocatedAmount { get; set; }

    public string? Description { get; set; }
}
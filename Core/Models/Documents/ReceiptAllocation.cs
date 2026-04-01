using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Documents;

public class ReceiptAllocation : AuditableEntity
{
    public int ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public int SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public decimal AllocatedAmount { get; set; }

    public string? Description { get; set; }
}
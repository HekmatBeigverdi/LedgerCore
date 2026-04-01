namespace LedgerCore.Core.ViewModels.ReceiptsPayments;

public class PaymentAllocationDto
{
    public int Id { get; set; }

    public int PurchaseInvoiceId { get; set; }
    public string? PurchaseInvoiceNumber { get; set; }

    public decimal AllocatedAmount { get; set; }

    public string? Description { get; set; }
}
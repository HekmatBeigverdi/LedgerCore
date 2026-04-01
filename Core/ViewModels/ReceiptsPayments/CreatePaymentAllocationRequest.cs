namespace LedgerCore.Core.ViewModels.ReceiptsPayments;

public class CreatePaymentAllocationRequest
{
    public int PurchaseInvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string? Description { get; set; }
}
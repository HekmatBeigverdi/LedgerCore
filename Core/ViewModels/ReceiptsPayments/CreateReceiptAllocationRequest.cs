namespace LedgerCore.Core.ViewModels.ReceiptsPayments;

public class CreateReceiptAllocationRequest
{
    public int SalesInvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string? Description { get; set; }
}
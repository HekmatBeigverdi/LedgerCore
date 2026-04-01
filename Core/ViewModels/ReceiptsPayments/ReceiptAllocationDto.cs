namespace LedgerCore.Core.ViewModels.ReceiptsPayments;

public class ReceiptAllocationDto
{
    public int Id { get; set; }

    public int SalesInvoiceId { get; set; }
    public string? SalesInvoiceNumber { get; set; }

    public decimal AllocatedAmount { get; set; }

    public string? Description { get; set; }
}
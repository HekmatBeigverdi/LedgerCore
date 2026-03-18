namespace LedgerCore.Core.ViewModels.ReceiptsPayments;

public class ReversePostedDocumentRequest
{
    public DateTime? ReversalDate { get; set; }
    public string? Description { get; set; }
}
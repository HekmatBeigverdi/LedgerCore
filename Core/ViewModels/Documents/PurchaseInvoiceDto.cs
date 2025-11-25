using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Documents;

public class PurchaseInvoiceDto
{
    public int Id { get; set; }
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }
    public DateTime? DueDate { get; set; }

    public int SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal FxRate { get; set; }

    public DocumentStatus Status { get; set; }

    public decimal TotalNetAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public int? JournalVoucherId { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = new();
}
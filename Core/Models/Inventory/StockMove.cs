using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Inventory;

public class StockMove: AuditableEntity
{
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public StockMoveType MoveType { get; set; }     // Inbound / Outbound / Adjustment

    public decimal Quantity { get; set; }           // مقدار مثبت (سمت In/Out را MoveType تعیین می‌کند)
    public decimal? UnitCost { get; set; }          // قیمت تمام‌شده واحد در لحظه حرکت

    // برای ارتباط با سند مرجع (فاکتور، رسید، تعدیل، ...)
    public string? RefDocumentType { get; set; }    // مثلا "SalesInvoice", "PurchaseInvoice", "InventoryAdjustment"
    public int? RefDocumentId { get; set; }
    public int? RefDocumentLineId { get; set; }
}
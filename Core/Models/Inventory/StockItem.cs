using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Inventory;

public class StockItem: BaseEntity
{
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal OnHand { get; set; }       // موجودی واقعی
    public decimal Reserved { get; set; }     // رزرو شده (برای سفارش‌ها)
    public decimal Available => OnHand - Reserved;

    public decimal AverageCost { get; set; }  // میانگین قیمت تمام‌شده
}
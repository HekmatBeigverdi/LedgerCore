using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class Product: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    public string? Barcode { get; set; }
    public bool IsService { get; set; } = false;

    public int? DefaultUnitId { get; set; }   // اگر بعداً بحث واحدها را اضافه کنیم
    public decimal? DefaultSalesPrice { get; set; }
    public decimal? DefaultPurchasePrice { get; set; }

    public int? DefaultTaxRateId { get; set; }
    public TaxRate? DefaultTaxRate { get; set; }

    public bool IsActive { get; set; } = true;
}
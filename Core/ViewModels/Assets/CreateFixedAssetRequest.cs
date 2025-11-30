namespace LedgerCore.Core.ViewModels.Assets;

public class CreateFixedAssetRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public int CategoryId { get; set; }
    public int DepreciationMethodId { get; set; }

    public DateTime AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }

    /// <summary>
    /// اگر صفر باشد، از DefaultUsefulLifeMonths دسته استفاده می‌شود.
    /// </summary>
    public int UsefulLifeMonths { get; set; }

    public decimal ResidualValue { get; set; }

    public int? BranchId { get; set; }
    public int? CostCenterId { get; set; }
    public int? ProjectId { get; set; }
}
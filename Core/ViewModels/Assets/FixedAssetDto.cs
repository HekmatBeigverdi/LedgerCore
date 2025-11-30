using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Assets;

public class FixedAssetDto
{
    public int Id { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public int DepreciationMethodId { get; set; }
    public string? DepreciationMethodName { get; set; }

    public DateTime AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }

    public int UsefulLifeMonths { get; set; }
    public decimal ResidualValue { get; set; }

    public AssetStatus Status { get; set; }

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    public int? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }

    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }

    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
}
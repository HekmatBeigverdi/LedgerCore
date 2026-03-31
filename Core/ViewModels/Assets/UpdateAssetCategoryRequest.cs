namespace LedgerCore.Core.ViewModels.Assets;

public class UpdateAssetCategoryRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DefaultUsefulLifeMonths { get; set; }
    public decimal DefaultResidualPercent { get; set; }
}
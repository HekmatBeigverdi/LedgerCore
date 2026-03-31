namespace LedgerCore.Core.ViewModels.Assets;

public class CreateAssetCategoryRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DefaultUsefulLifeMonths { get; set; }
    public decimal DefaultResidualPercent { get; set; }
}
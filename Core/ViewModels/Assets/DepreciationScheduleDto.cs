namespace LedgerCore.Core.ViewModels.Assets;

public class DepreciationScheduleDto
{
    public int Id { get; set; }
    public int FixedAssetId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }

    public bool IsPosted { get; set; }
    public int? JournalVoucherId { get; set; }
}
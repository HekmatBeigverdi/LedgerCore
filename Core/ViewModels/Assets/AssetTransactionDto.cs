using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Assets;

public class AssetTransactionDto
{
    public int Id { get; set; }
    public int FixedAssetId { get; set; }

    public AssetTransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    public int? JournalVoucherId { get; set; }
}
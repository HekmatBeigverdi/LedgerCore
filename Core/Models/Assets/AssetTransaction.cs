using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Assets;

public class AssetTransaction: AuditableEntity
{
    public int FixedAssetId { get; set; }
    public FixedAsset? FixedAsset { get; set; }

    public AssetTransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }         // مبلغ (مثلاً افزایش ارزش، مبلغ فروش، ...)
    public string? Description { get; set; }

    public int? JournalVoucherId { get; set; }  // سند حسابداری مرتبط
    public JournalVoucher? JournalVoucher { get; set; }
}
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Assets;

public class FixedAsset: AuditableEntity
{
    public string Code { get; set; } = default!;      // کد دارایی
    public string Name { get; set; } = default!;      // نام دارایی

    public int CategoryId { get; set; }
    public AssetCategory? Category { get; set; }

    public int DepreciationMethodId { get; set; }
    public DepreciationMethod? DepreciationMethod { get; set; }

    public DateTime AcquisitionDate { get; set; }     // تاریخ تحصیل
    public decimal AcquisitionCost { get; set; }      // بهای تمام‌شده اولیه

    /// <summary>
    /// عمر مفید به ماه (می‌تواند override پیش‌فرض دسته باشد)
    /// </summary>
    public int UsefulLifeMonths { get; set; }

    /// <summary>
    /// ارزش اسقاط (مبلغ ثابت، نه درصد)
    /// </summary>
    public decimal ResidualValue { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Active;

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// استهلاک انباشته فعلی (برای سرعت در محاسبه)
    /// </summary>
    public decimal AccumulatedDepreciation { get; set; }

    /// <summary>
    /// ارزش دفتری فعلی = AcquisitionCost - AccumulatedDepreciation
    /// </summary>
    public decimal NetBookValue => AcquisitionCost - AccumulatedDepreciation;

    public ICollection<DepreciationSchedule> DepreciationSchedules { get; set; }
        = new List<DepreciationSchedule>();

    public ICollection<AssetTransaction> Transactions { get; set; }
        = new List<AssetTransaction>();
}
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Inventory;

public class Warehouse: AuditableEntity
{
    public string Code { get; set; } = default!;    // کد انبار
    public string Name { get; set; } = default!;    // نام انبار
    public string? Address { get; set; }

    public int? BranchId { get; set; }              // انبار مربوط به کدام شعبه است
    public Branch? Branch { get; set; }

    public bool IsActive { get; set; } = true;
}
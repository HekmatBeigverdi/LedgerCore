namespace LedgerCore.Core.Models.Common;

public abstract class AuditableEntity: BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
}
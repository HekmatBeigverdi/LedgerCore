namespace LedgerCore.Core.Models.Master;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public int BranchId { get; set; }
    public bool IsActive { get; set; }
}
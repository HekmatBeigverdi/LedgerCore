namespace LedgerCore.Core.Models.Master;

public class BranchDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; }
}
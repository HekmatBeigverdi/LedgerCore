namespace LedgerCore.Core.Models.Master;

public class NumberSeriesDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = default!;
    public string Code { get; set; } = default!;
    public int? BranchId { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public int Padding { get; set; }
    public long CurrentNumber { get; set; }
    public bool IsActive { get; set; }
}
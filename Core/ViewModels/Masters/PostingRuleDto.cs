namespace LedgerCore.Core.ViewModels.Masters;

public class PostingRuleDto
{
    public int Id { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DocumentType { get; set; } = default!;

    public int? BranchId { get; set; }

    public bool IsActive { get; set; }
    public bool AutoPost { get; set; }
    public int Priority { get; set; }

    public List<PostingRuleLineDto> Lines { get; set; } = new();
}
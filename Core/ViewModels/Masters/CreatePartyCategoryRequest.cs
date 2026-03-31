namespace LedgerCore.Core.ViewModels.Masters;

public class CreatePartyCategoryRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
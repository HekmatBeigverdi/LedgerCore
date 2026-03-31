namespace LedgerCore.Core.ViewModels.Masters;

public class UpdateBankRequest
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
}
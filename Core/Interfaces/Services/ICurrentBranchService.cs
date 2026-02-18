namespace LedgerCore.Core.Interfaces.Services;

public interface ICurrentBranchService
{
    int? GetCurrentBranchId();
    int GetRequiredBranchId();
}
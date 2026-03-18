namespace LedgerCore.Core.Interfaces.Services;

public interface INumberSeriesService
{
    Task<string> NextAsync(string entityType, int? branchId, CancellationToken cancellationToken = default);
}
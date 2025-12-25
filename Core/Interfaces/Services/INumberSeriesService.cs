namespace LedgerCore.Core.Interfaces.Services;

public interface INumberSeriesService
{
    Task<string> NextAsync(string seriesCode, int? branchId, CancellationToken cancellationToken = default);
}
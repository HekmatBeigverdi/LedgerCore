using LedgerCore.Core.Models.Workflow;

namespace LedgerCore.Core.Interfaces.Services;

public interface IApprovalService
{
    Task<ApprovalRequest> CreateApprovalRequestAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default);

    Task ApproveAsync(
        int approvalRequestId,
        string performedBy,
        string? comment,
        CancellationToken cancellationToken = default);

    Task RejectAsync(
        int approvalRequestId,
        string performedBy,
        string? comment,
        CancellationToken cancellationToken = default);
}
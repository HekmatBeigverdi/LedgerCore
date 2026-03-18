namespace LedgerCore.Core.Interfaces.Services;

public interface ISecurityActivityLogService
{
    Task LogAsync(
        string action,
        string entityType,
        int entityId,
        int? actorUserId,
        string? actorUserName,
        string? details,
        CancellationToken cancellationToken = default);
}
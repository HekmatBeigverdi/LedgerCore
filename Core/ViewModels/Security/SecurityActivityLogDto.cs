namespace LedgerCore.Core.ViewModels.Security;

public class SecurityActivityLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public int EntityId { get; set; }
    public int? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
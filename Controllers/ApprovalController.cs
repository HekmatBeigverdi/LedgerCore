using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApprovalController(
    IApprovalService approvalService,
    IUnitOfWork uow)
    : ControllerBase
{
    // برای سادگی، fromBody مدل‌های ساده تعریف می‌کنیم

    public record CreateApprovalRequestDto(
        string EntityType,
        int EntityId);

    public record ApprovalActionDto(
        string PerformedBy,
        string? Comment);

    /// <summary>
    /// ایجاد درخواست تأیید برای یک سند (مثلاً SalesInvoice, PurchaseInvoice و ...).
    /// اگر قبلاً درخواست Pending موجود باشد، همان را برمی‌گرداند.
    /// </summary>
    [HttpPost("requests")]
    [HasPermission("Approval.Request.Create")]
    public async Task<ActionResult<ApprovalRequest>> CreateRequest(
        [FromBody] CreateApprovalRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.EntityType) || dto.EntityId <= 0)
            return BadRequest("EntityType and EntityId are required.");

        var request = await approvalService.CreateApprovalRequestAsync(
            dto.EntityType,
            dto.EntityId,
            cancellationToken);

        return Ok(request);
    }

    /// <summary>
    /// دریافت یک ApprovalRequest بر اساس Id.
    /// </summary>
    [HttpGet("requests/{id:int}")]
    [HasPermission("Approval.Request.View")]
    public async Task<ActionResult<ApprovalRequest>> GetRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<ApprovalRequest>();
        var request = await repo.GetByIdAsync(id, cancellationToken);

        if (request is null)
            return NotFound();

        return Ok(request);
    }

    /// <summary>
    /// لیست درخواست‌های تأیید برای یک سند خاص (EntityType + EntityId).
    /// معمولاً یک مورد خواهیم داشت، ولی اجازه می‌دهیم چندتا باشد.
    /// </summary>
    [HttpGet("requests/by-entity")]
    [HasPermission("Approval.Request.View")]
    public async Task<ActionResult<IReadOnlyList<ApprovalRequest>>> GetRequestsByEntity(
        [FromQuery] string entityType,
        [FromQuery] int entityId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            return BadRequest("EntityType and EntityId are required.");

        var repo = uow.Repository<ApprovalRequest>();
        var page = await repo.FindAsync(
            x => x.EntityType == entityType && x.EntityId == entityId,
            null,
            cancellationToken);

        return Ok(page.Items);
    }

    /// <summary>
    /// تأیید یک درخواست تأیید.
    /// </summary>
    [HttpPost("requests/{id:int}/approve")]
    [HasPermission("Approval.Request.Approve")]
    public async Task<ActionResult> Approve(
        int id,
        [FromBody] ApprovalActionDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.PerformedBy))
            return BadRequest("PerformedBy is required.");

        await approvalService.ApproveAsync(id, dto.PerformedBy, dto.Comment, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// رد کردن یک درخواست تأیید.
    /// </summary>
    [HttpPost("requests/{id:int}/reject")]
    [HasPermission("Approval.Request.Reject")]
    public async Task<ActionResult> Reject(
        int id,
        [FromBody] ApprovalActionDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.PerformedBy))
            return BadRequest("PerformedBy is required.");

        await approvalService.RejectAsync(id, dto.PerformedBy, dto.Comment, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// لیست درخواست‌های Pending (مثلاً برای نمایش Inbox تأیید).
    /// </summary>
    [HttpGet("requests/pending")]
    [HasPermission("Approval.Request.View")]
    public async Task<ActionResult<IReadOnlyList<ApprovalRequest>>> GetPending(
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<ApprovalRequest>();
        var page = await repo.FindAsync(
            x => x.Status == ApprovalStatus.Pending,
            null,
            cancellationToken);

        return Ok(page.Items);
    }
}
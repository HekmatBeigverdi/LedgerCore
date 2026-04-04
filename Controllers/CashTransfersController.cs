using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Workflow;
using LedgerCore.Core.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CashTransfersController(
    ICashTransferService cashTransferService,
    IApprovalService approvalService,
    IMapper mapper)
    : ControllerBase
{
    // GET api/cashtransfers/{id}
    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.CashTransfer_View)]
    public async Task<ActionResult<CashTransferDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var transfer = await cashTransferService.GetCashTransferAsync(id, cancellationToken);
        if (transfer is null)
            return NotFound();

        var dto = mapper.Map<CashTransferDto>(transfer);
        return Ok(dto);
    }
    
    [HttpPost("{id:int}/submit")]
    [HasPermission(PermissionCodes.Approval_Request_Create)]
    public async Task<ActionResult<ApprovalRequest>> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        var transfer = await cashTransferService.GetCashTransferAsync(id, cancellationToken);
        if (transfer is null)
            return NotFound();

        var request = await approvalService.CreateApprovalRequestAsync(
            "CashTransfer",
            id,
            cancellationToken);

        return Ok(request);
    }

    // POST api/cashtransfers
    [HttpPost]
    [HasPermission(PermissionCodes.CashTransfer_Create)]
    public async Task<ActionResult<CashTransferDto>> Create(
        [FromBody] CreateCashTransferRequest request,
        CancellationToken cancellationToken)
    {
        // تبدیل Request به Entity
        var entity = mapper.Map<CashTransfer>(request);

        // استفاده از سرویس دامین برای ایجاد سند (شماره‌دهی و ولیدیشن داخل سرویس انجام می‌شود)
        var created = await cashTransferService.CreateCashTransferAsync(entity, cancellationToken);

        var dto = mapper.Map<CashTransferDto>(created);

        return CreatedAtAction(
            nameof(Get),
            new { id = dto.Id },
            dto);
    }

    // POST api/cashtransfers/{id}/post
    [HttpPost("{id:int}/post")]
    [HasPermission(PermissionCodes.CashTransfer_Post)]
    public async Task<IActionResult> Post(
        int id,
        CancellationToken cancellationToken)
    {
        await cashTransferService.PostCashTransferAsync(id, cancellationToken);
        return NoContent();
    }
}
using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Workflow;
using LedgerCore.Core.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PurchaseReturnsController(
    IPurchaseService purchaseService,
    IApprovalService approvalService,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.Purchase_Return_View)]
    public async Task<ActionResult> Query([FromQuery] PagingParams paging, CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var result = await uow.Invoices.QueryPurchaseReturnsAsync(
            paging,
            x => x.BranchId == branchId,
            cancellationToken);

        var dto = result.Items.Select(mapper.Map<PurchaseReturnDto>).ToList();

        return Ok(new
        {
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            Items = dto
        });
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Purchase_Return_View)]
    public async Task<ActionResult<PurchaseReturnDto>> Get(int id, CancellationToken cancellationToken)
    {
        var document = await purchaseService.GetPurchaseReturnAsync(id, cancellationToken);
        if (document is null)
            return NotFound();

        var dto = mapper.Map<PurchaseReturnDto>(document);
        return Ok(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Purchase_Return_Create)]
    public async Task<ActionResult<PurchaseReturnDto>> Create(
        [FromBody] CreatePurchaseReturnRequest request,
        CancellationToken cancellationToken)
    {
        var document = mapper.Map<PurchaseReturn>(request);

        document.Lines = request.Lines
            .Select(l => mapper.Map<InvoiceLine>(l))
            .ToList();

        var created = await purchaseService.CreatePurchaseReturnAsync(document, cancellationToken);
        var dto = mapper.Map<PurchaseReturnDto>(created);

        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Purchase_Return_Edit)]
    public async Task<ActionResult<PurchaseReturnDto>> Update(
        int id,
        [FromBody] UpdatePurchaseReturnRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await purchaseService.GetPurchaseReturnAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing);

        existing.Lines.Clear();
        foreach (var lineReq in request.Lines)
        {
            var line = mapper.Map<InvoiceLine>(lineReq);
            existing.Lines.Add(line);
        }

        var updated = await purchaseService.UpdatePurchaseReturnAsync(existing, cancellationToken);
        var dto = mapper.Map<PurchaseReturnDto>(updated);

        return Ok(dto);
    }

    [HttpPost("{id:int}/submit")]
    [HasPermission(PermissionCodes.Approval_Request_Create)]
    public async Task<ActionResult<ApprovalRequest>> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        var document = await purchaseService.GetPurchaseReturnAsync(id, cancellationToken);
        if (document is null)
            return NotFound();

        var request = await approvalService.CreateApprovalRequestAsync(
            "PurchaseReturn",
            id,
            cancellationToken);

        return Ok(request);
    }

    [HttpPost("{id:int}/post")]
    [HasPermission(PermissionCodes.Purchase_Return_Post)]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await purchaseService.PostPurchaseReturnAsync(id, cancellationToken);
        return NoContent();
    }
}
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
public class SalesReturnsController(
    ISalesService salesService,
    IApprovalService approvalService,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.Sales_Return_View)]
    public async Task<ActionResult> Query([FromQuery] PagingParams paging, CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var result = await uow.Invoices.QuerySalesReturnsAsync(
            paging,
            x => x.BranchId == branchId,
            cancellationToken);

        var dto = result.Items.Select(mapper.Map<SalesReturnDto>).ToList();

        return Ok(new
        {
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            Items = dto
        });
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Sales_Return_View)]
    public async Task<ActionResult<SalesReturnDto>> Get(int id, CancellationToken cancellationToken)
    {
        var document = await salesService.GetSalesReturnAsync(id, cancellationToken);
        if (document is null)
            return NotFound();

        var dto = mapper.Map<SalesReturnDto>(document);
        return Ok(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Sales_Return_Create)]
    public async Task<ActionResult<SalesReturnDto>> Create(
        [FromBody] CreateSalesReturnRequest request,
        CancellationToken cancellationToken)
    {
        var document = mapper.Map<SalesReturn>(request);

        document.Lines = request.Lines
            .Select(l => mapper.Map<InvoiceLine>(l))
            .ToList();

        var created = await salesService.CreateSalesReturnAsync(document, cancellationToken);
        var dto = mapper.Map<SalesReturnDto>(created);

        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Sales_Return_Edit)]
    public async Task<ActionResult<SalesReturnDto>> Update(
        int id,
        [FromBody] UpdateSalesReturnRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await salesService.GetSalesReturnAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing);

        existing.Lines.Clear();
        foreach (var lineReq in request.Lines)
        {
            var line = mapper.Map<InvoiceLine>(lineReq);
            existing.Lines.Add(line);
        }

        var updated = await salesService.UpdateSalesReturnAsync(existing, cancellationToken);
        var dto = mapper.Map<SalesReturnDto>(updated);

        return Ok(dto);
    }

    [HttpPost("{id:int}/submit")]
    [HasPermission(PermissionCodes.Approval_Request_Create)]
    public async Task<ActionResult<ApprovalRequest>> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        var document = await salesService.GetSalesReturnAsync(id, cancellationToken);
        if (document is null)
            return NotFound();

        var request = await approvalService.CreateApprovalRequestAsync(
            "SalesReturn",
            id,
            cancellationToken);

        return Ok(request);
    }

    [HttpPost("{id:int}/post")]
    [HasPermission(PermissionCodes.Sales_Return_Post)]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await salesService.PostSalesReturnAsync(id, cancellationToken);
        return NoContent();
    }
}
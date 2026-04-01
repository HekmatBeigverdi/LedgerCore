using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Documents;
using LedgerCore.Core.ViewModels.ReceiptsPayments;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReceiptsController(
    IAccountingService accountingService,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    // GET api/receipts/{id}
    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Receipt_View)]
    public async Task<ActionResult<ReceiptDto>> Get(int id, CancellationToken cancellationToken)
    {
        var receipt = await accountingService.GetReceiptAsync(id, cancellationToken);
        if (receipt is null)
            return NotFound();

        var dto = mapper.Map<ReceiptDto>(receipt);
        return Ok(dto);
    }

    // GET api/receipts?PageNumber=1&PageSize=20
    [HttpGet]
    [HasPermission(PermissionCodes.Receipt_View)]
    public async Task<ActionResult<PagedResult<ReceiptDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        
        var branchId = currentBranch.GetRequiredBranchId();
        var result = await uow.Receipts.QueryAsync(branchId, paging, cancellationToken);
        var dtoItems = result.Items.Select(x => mapper.Map<ReceiptDto>(x)).ToList();

        var dtoPage = new PagedResult<ReceiptDto>(
            dtoItems,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dtoPage);
    }

    // POST api/receipts
    [HttpPost]
    [HasPermission(PermissionCodes.Receipt_Create)]
    public async Task<ActionResult<ReceiptDto>> Create(
        [FromBody] CreateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = mapper.Map<Receipt>(request);
        
        receipt.PartyId = request.CustomerId;
        receipt.Method = request.PaymentMethod;
        
        receipt.Allocations = request.Allocations
            .Select(x => new ReceiptAllocation
            {
                SalesInvoiceId = x.SalesInvoiceId,
                AllocatedAmount = x.AllocatedAmount,
                Description = x.Description
            })
            .ToList();
        
        
        var created = await accountingService.CreateReceiptAsync(receipt, cancellationToken);

        var dto = mapper.Map<ReceiptDto>(created);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // PUT api/receipts/{id}
    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Receipt_Edit)]
    public async Task<ActionResult<ReceiptDto>> Update(
        int id,
        [FromBody] UpdateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await accountingService.GetReceiptAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing);
        var updated = await accountingService.UpdateReceiptAsync(existing, cancellationToken);

        var dto = mapper.Map<ReceiptDto>(updated);
        return Ok(dto);
    }

    // POST api/receipts/{id}/post
    [HttpPost("{id:int}/post")]
    [HasPermission(PermissionCodes.Receipt_Post)]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await accountingService.PostReceiptAsync(id, cancellationToken);
        return NoContent();
    }
    
    [HttpPost("{id:int}/reverse")]
    [HasPermission(PermissionCodes.Receipt_Reverse)]
    public async Task<ActionResult<ReceiptDto>> Reverse(
        int id,
        [FromBody] ReversePostedDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = await accountingService.ReverseReceiptAsync(
            id,
            request.ReversalDate,
            request.Description,
            cancellationToken);

        return Ok(mapper.Map<ReceiptDto>(receipt));
    }
    

}
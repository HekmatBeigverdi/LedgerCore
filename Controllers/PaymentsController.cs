using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Documents;
using LedgerCore.Core.ViewModels.ReceiptsPayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentsController(
    IAccountingService accountingService,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    // GET api/v1/payments/{id}
    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Payment_View)]
    public async Task<ActionResult<PaymentDto>> Get(int id, CancellationToken cancellationToken)
    {
        var payment = await accountingService.GetPaymentAsync(id, cancellationToken);
        if (payment is null)
            return NotFound();

        var dto = mapper.Map<PaymentDto>(payment);
        return Ok(dto);
    }

    // GET api/v1/payments?PageNumber=1&PageSize=20
    [HttpGet]
    [HasPermission(PermissionCodes.Payment_View)]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();
        var result = await uow.Payments.QueryAsync(branchId, paging, cancellationToken);
        
        var dtoItems = result.Items.Select(x => mapper.Map<PaymentDto>(x)).ToList();

        var dtoPage = new PagedResult<PaymentDto>(
            dtoItems,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dtoPage);
    }

    // POST api/v1/payments
    [HttpPost]
    [HasPermission(PermissionCodes.Payment_Create)]
    public async Task<ActionResult<PaymentDto>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = mapper.Map<Payment>(request);

        payment.PartyId = request.SupplierId;
        payment.Method = request.PaymentMethod;

        payment.Allocations = (request.Allocations ?? [])
            .Select(x => new PaymentAllocation
            {
                PurchaseInvoiceId = x.PurchaseInvoiceId,
                AllocatedAmount = x.AllocatedAmount,
                Description = x.Description
            })
            .ToList();

        var created = await accountingService.CreatePaymentAsync(payment, cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = created.Id },
            mapper.Map<PaymentDto>(created));
    }

    // PUT api/v1/payments/{id}
    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Payment_Edit)]
    public async Task<ActionResult<PaymentDto>> Update(
        int id,
        [FromBody] UpdatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = mapper.Map<Payment>(request);
        payment.Id = id;

        payment.PartyId = request.SupplierId;
        payment.Method = request.PaymentMethod;

        payment.Allocations = (request.Allocations ?? [])
            .Select(x => new PaymentAllocation
            {
                PurchaseInvoiceId = x.PurchaseInvoiceId,
                AllocatedAmount = x.AllocatedAmount,
                Description = x.Description
            })
            .ToList();

        var updated = await accountingService.UpdatePaymentAsync(payment, cancellationToken);

        return Ok(mapper.Map<PaymentDto>(updated));
    }

    // POST api/v1/payments/{id}/post
    [HttpPost("{id:int}/post")]
    [HasPermission(PermissionCodes.Payment_Post)]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await accountingService.PostPaymentAsync(id, cancellationToken);
        return NoContent();
    }
    
    [HttpPost("{id:int}/reverse")]
    [HasPermission(PermissionCodes.Payment_Reverse)]
    public async Task<ActionResult<PaymentDto>> Reverse(
        int id,
        [FromBody] ReversePostedDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await accountingService.ReversePaymentAsync(
            id,
            request.ReversalDate,
            request.Description,
            cancellationToken);

        return Ok(mapper.Map<PaymentDto>(payment));
    }

}
using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;
using LedgerCore.Core.ViewModels.ReceiptsPayments;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    IAccountingService accountingService,
    IUnitOfWork uow,
    IMapper mapper)
    : ControllerBase
{
    // GET api/payments/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Get(int id, CancellationToken cancellationToken)
    {
        var payment = await accountingService.GetPaymentAsync(id, cancellationToken);
        if (payment is null)
            return NotFound();

        var dto = mapper.Map<PaymentDto>(payment);
        return Ok(dto);
    }

    // GET api/payments?PageNumber=1&PageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Payments.QueryAsync(paging, cancellationToken);
        var dtoItems = result.Items.Select(x => mapper.Map<PaymentDto>(x)).ToList();

        var dtoPage = new PagedResult<PaymentDto>(
            dtoItems,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dtoPage);
    }

    // POST api/payments
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = mapper.Map<Payment>(request);
        var created = await accountingService.CreatePaymentAsync(payment, cancellationToken);

        var dto = mapper.Map<PaymentDto>(created);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // PUT api/payments/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Update(
        int id,
        [FromBody] UpdatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await accountingService.GetPaymentAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing);
        var updated = await accountingService.UpdatePaymentAsync(existing, cancellationToken);

        var dto = mapper.Map<PaymentDto>(updated);
        return Ok(dto);
    }

    // POST api/payments/{id}/post
    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await accountingService.PostPaymentAsync(id, cancellationToken);
        return NoContent();
    }
}
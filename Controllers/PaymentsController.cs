using AutoMapper;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;
using LedgerCore.Core.ViewModels.ReceiptsPayments;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IAccountingService accountingService, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Get(int id, CancellationToken cancellationToken)
    {
        var payment = await accountingService.GetPaymentAsync(id, cancellationToken);
        if (payment is null)
            return NotFound();

        return Ok(mapper.Map<PaymentDto>(payment));
    }

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

        return Ok(mapper.Map<PaymentDto>(updated));
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await accountingService.PostPaymentAsync(id, cancellationToken);
        return NoContent();
    }
}
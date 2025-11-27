using AutoMapper;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;
using LedgerCore.Core.ViewModels.ReceiptsPayments;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceiptsController(IAccountingService accountingService, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReceiptDto>> Get(int id, CancellationToken cancellationToken)
    {
        var receipt = await accountingService.GetReceiptAsync(id, cancellationToken);
        if (receipt is null)
            return NotFound();

        return Ok(mapper.Map<ReceiptDto>(receipt));
    }

    [HttpPost]
    public async Task<ActionResult<ReceiptDto>> Create(
        [FromBody] CreateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = mapper.Map<Receipt>(request);
        var created = await accountingService.CreateReceiptAsync(receipt, cancellationToken);

        var dto = mapper.Map<ReceiptDto>(created);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
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

        return Ok(mapper.Map<ReceiptDto>(updated));
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await accountingService.PostReceiptAsync(id, cancellationToken);
        return NoContent();
    }
}
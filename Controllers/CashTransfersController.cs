using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashTransfersController(
    IUnitOfWork uow,
    ICashTransferService cashTransferService,
    IMapper mapper)
    : ControllerBase
{
    // GET api/cashtransfers/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CashTransferDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<CashTransfer>();
        var transfer = await repo.GetByIdAsync(id, cancellationToken);
        if (transfer is null)
            return NotFound();

        var dto = mapper.Map<CashTransferDto>(transfer);
        return Ok(dto);
    }

    // POST api/cashtransfers
    [HttpPost]
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
    public async Task<IActionResult> Post(
        int id,
        CancellationToken cancellationToken)
    {
        await cashTransferService.PostCashTransferAsync(id, cancellationToken);
        return NoContent();
    }
}
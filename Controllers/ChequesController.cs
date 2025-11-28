using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Cheques;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChequesController(IUnitOfWork uow, IChequeService chequeService, IMapper mapper)
    : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChequeDto>> Get(int id, CancellationToken cancellationToken)
    {
        var cheque = await uow.Cheques.GetByIdAsync(id, cancellationToken);
        if (cheque is null)
            return NotFound();

        // اگر خواستی اینجا Include برای History و Party و BankAccount اضافه کن (در Repo)
        return Ok(mapper.Map<ChequeDto>(cheque));
    }

    [HttpPost]
    public async Task<ActionResult<ChequeDto>> Register(
        [FromBody] RegisterChequeRequest request,
        CancellationToken cancellationToken)
    {
        var cheque = mapper.Map<Cheque>(request);
        var created = await chequeService.RegisterChequeAsync(cheque, cancellationToken);

        var dto = mapper.Map<ChequeDto>(created);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int id,
        [FromBody] ChangeChequeStatusRequest request,
        CancellationToken cancellationToken)
    {
        await chequeService.ChangeStatusAsync(id, request.NewStatus, request.Comment, cancellationToken);
        return NoContent();
    }
}
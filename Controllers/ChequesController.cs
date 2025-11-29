using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.ViewModels.Cheques;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChequesController(
    IUnitOfWork uow,
    IChequeService chequeService,
    IMapper mapper)
    : ControllerBase
{
    // GET api/cheques/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChequeDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var cheque = await uow.Cheques.GetByIdAsync(id, cancellationToken);
        if (cheque is null)
            return NotFound();

        var dto = mapper.Map<ChequeDto>(cheque);
        return Ok(dto);
    }

    // GET api/cheques?PageNumber=1&PageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<ChequeDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Cheques.QueryAsync(paging, cancellationToken);
        var dtoItems = result.Items.Select(c => mapper.Map<ChequeDto>(c)).ToList();

        var dtoPage = new PagedResult<ChequeDto>(
            dtoItems,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dtoPage);
    }

    // GET api/cheques/status/{status}
    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<ChequeDto>>> GetByStatus(
        ChequeStatus status,
        CancellationToken cancellationToken)
    {
        var list = await uow.Cheques.GetByStatusAsync(status, cancellationToken);
        var dtoList = list.Select(c => mapper.Map<ChequeDto>(c)).ToList();
        return Ok(dtoList);
    }

    // POST api/cheques
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

    // POST api/cheques/{id}/status
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
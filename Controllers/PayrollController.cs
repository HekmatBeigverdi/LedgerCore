using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.ViewModels.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayrollController(
    IUnitOfWork uow,
    IPayrollService payrollService,
    IMapper mapper)
    : ControllerBase
{
    // GET api/payroll/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PayrollDocumentDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var payroll = await uow.Payrolls.GetWithLinesAsync(id, cancellationToken);
        if (payroll is null)
            return NotFound();

        var dto = mapper.Map<PayrollDocumentDto>(payroll);
        return Ok(dto);
    }

    // GET api/payroll?PageNumber=1&PageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<PayrollDocumentDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Payrolls.QueryAsync(paging, cancellationToken);

        var dtoItems = result.Items
            .Select(p => mapper.Map<PayrollDocumentDto>(p))
            .ToList();

        var dtoPage = new PagedResult<PayrollDocumentDto>(
            dtoItems,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dtoPage);
    }

    // POST api/payroll
    // ایجاد + محاسبه سند حقوق
    [HttpPost]
    public async Task<ActionResult<PayrollDocumentDto>> CreateAndCalculate(
        [FromBody] CreatePayrollRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var payroll = mapper.Map<PayrollDocument>(request);

        var calculated = await payrollService.CalculatePayrollAsync(
            payroll,
            cancellationToken);

        var dto = mapper.Map<PayrollDocumentDto>(calculated);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // POST api/payroll/{id}/post
    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(
        int id,
        CancellationToken cancellationToken)
    {
        await payrollService.PostPayrollAsync(id, cancellationToken);
        return NoContent();
    }
}
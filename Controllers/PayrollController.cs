using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PayrollController(
    IUnitOfWork uow,
    IPayrollService payrollService,
    IMapper mapper,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    // GET api/payroll/{id}
    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Payroll_View)]
    public async Task<ActionResult<PayrollDocumentDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();
        var payroll = await uow.Payrolls.GetWithLinesAsync(id, branchId, cancellationToken);
        
        if (payroll is null)
            return NotFound();

        var dto = mapper.Map<PayrollDocumentDto>(payroll);
        return Ok(dto);
    }

    // GET api/payroll?PageNumber=1&PageSize=20
    [HttpGet]
    [HasPermission(PermissionCodes.Payroll_View)]
    public async Task<ActionResult<PagedResult<PayrollDocumentDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();
        var result = await uow.Payrolls.QueryAsync(branchId, paging, cancellationToken);

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
    [HasPermission(PermissionCodes.Payroll_Manage)]
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
    [HasPermission(PermissionCodes.Payroll_Post)]
    public async Task<IActionResult> Post(
        int id,
        CancellationToken cancellationToken)
    {
        await payrollService.PostPayrollAsync(id, cancellationToken);
        return NoContent();
    }
}
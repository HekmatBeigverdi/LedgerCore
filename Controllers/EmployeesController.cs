using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.ViewModels.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Employee>()
            .GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<EmployeeDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<Employee>()
            .GetAllAsync(paging, cancellationToken);

        var items = result.Items
            .Select(mapper.Map<EmployeeDto>)
            .ToList();

        return Ok(new PagedResult<EmployeeDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(
            request.PersonnelCode,
            request.HireDate,
            request.TerminationDate,
            request.BranchId,
            request.CostCenterId,
            null,
            cancellationToken);

        if (validation is not null)
            return validation;

        var entity = mapper.Map<Employee>(request);
        entity.PersonnelCode = request.PersonnelCode.Trim().ToUpperInvariant();
        entity.FullName = request.FullName.Trim();
        entity.NationalId = string.IsNullOrWhiteSpace(request.NationalId) ? null : request.NationalId.Trim();
        entity.InsuranceCode = string.IsNullOrWhiteSpace(request.InsuranceCode) ? null : request.InsuranceCode.Trim();

        await uow.Repository<Employee>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await uow.Repository<Employee>()
            .GetByIdAsync(entity.Id, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<EmployeeDto>(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(
        int id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Employee>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(
            request.PersonnelCode,
            request.HireDate,
            request.TerminationDate,
            request.BranchId,
            request.CostCenterId,
            id,
            cancellationToken);

        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.PersonnelCode = request.PersonnelCode.Trim().ToUpperInvariant();
        entity.FullName = request.FullName.Trim();
        entity.NationalId = string.IsNullOrWhiteSpace(request.NationalId) ? null : request.NationalId.Trim();
        entity.InsuranceCode = string.IsNullOrWhiteSpace(request.InsuranceCode) ? null : request.InsuranceCode.Trim();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await repo.GetByIdAsync(entity.Id, cancellationToken);

        return Ok(mapper.Map<EmployeeDto>(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Employee>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var isUsed = await uow.Repository<PayrollLine>()
            .AnyAsync(x => x.EmployeeId == id, cancellationToken);

        if (isUsed)
        {
            entity.IsActive = false;
            repo.Update(entity);
        }
        else
        {
            repo.Remove(entity);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string personnelCode,
        DateTime hireDate,
        DateTime? terminationDate,
        int? branchId,
        int? costCenterId,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(personnelCode))
            return BadRequest("PersonnelCode is required.");

        if (hireDate == default)
            return BadRequest("HireDate is required.");

        if (terminationDate.HasValue && terminationDate.Value.Date < hireDate.Date)
            return BadRequest("TerminationDate cannot be earlier than HireDate.");

        var normalizedCode = personnelCode.Trim().ToUpperInvariant();

        var duplicate = await uow.Repository<Employee>()
            .AnyAsync(
                x => x.PersonnelCode == normalizedCode &&
                     (!currentId.HasValue || x.Id != currentId.Value),
                cancellationToken);

        if (duplicate)
            return BadRequest("PersonnelCode already exists.");

        if (branchId.HasValue)
        {
            var branch = await uow.Repository<Branch>().GetByIdAsync(branchId.Value, cancellationToken);
            if (branch is null)
                return BadRequest("BranchId is invalid.");
        }

        if (costCenterId.HasValue)
        {
            var costCenter = await uow.Repository<CostCenter>().GetByIdAsync(costCenterId.Value, cancellationToken);
            if (costCenter is null)
                return BadRequest("CostCenterId is invalid.");
        }

        return null;
    }
}
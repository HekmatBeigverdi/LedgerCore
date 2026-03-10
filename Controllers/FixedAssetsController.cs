using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.ViewModels.Assets;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FixedAssetsController(
    IFixedAssetRepository fixedAssets,
    IAssetService assetService,
    IMapper mapper,
    IUnitOfWork uow,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    private readonly IUnitOfWork _uow = uow;

    // GET api/fixedassets/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FixedAssetDto>> Get(int id, CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var page = await _uow.Repository<FixedAsset>().FindAsync(
            x => x.Id == id && x.BranchId == branchId,
            null,
            cancellationToken);

        var asset = page.Items.FirstOrDefault();
        if (asset is null)
            return NotFound();

        var dto = mapper.Map<FixedAssetDto>(asset);
        return Ok(dto);
    }

    // GET api/fixedassets
    [HttpGet]
    public async Task<ActionResult<PagedResult<FixedAssetDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();
        var result = await fixedAssets.QueryAsync(branchId, paging, cancellationToken);
        var dtoItems = result.Items.Select(a => mapper.Map<FixedAssetDto>(a)).ToList();

        var dtoPage = new PagedResult<FixedAssetDto>(
            dtoItems,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dtoPage);
    }

    // POST api/fixedassets
    [HttpPost]
    public async Task<ActionResult<FixedAssetDto>> Create(
        [FromBody] CreateFixedAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset = mapper.Map<FixedAsset>(request);
        var created = await assetService.CreateFixedAssetAsync(asset, cancellationToken);

        var dto = mapper.Map<FixedAssetDto>(created);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // PUT api/fixedassets/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FixedAssetDto>> Update(
        int id,
        [FromBody] UpdateFixedAssetRequest request,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var page = await _uow.Repository<FixedAsset>().FindAsync(
            x => x.Id == id && x.BranchId == branchId,
            null,
            cancellationToken);

        var existing = page.Items.FirstOrDefault();

        if (existing is null)
            return NotFound();

        if (request.BranchId != 0 && request.BranchId != branchId)
            return BadRequest("BranchId cannot be changed across branches.");

        mapper.Map(request, existing);

        if (existing.BranchId != branchId)
            return BadRequest("BranchId is not valid for current branch scope.");

        fixedAssets.Update(existing);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<FixedAssetDto>(existing);
        return Ok(dto);
    }

    // POST api/fixedassets/{id}/schedule
    [HttpPost("{id:int}/schedule")]
    public async Task<IActionResult> GenerateSchedule(int id, CancellationToken cancellationToken)
    {
        await assetService.GenerateDepreciationScheduleAsync(id, cancellationToken);
        return NoContent();
    }

    // GET api/fixedassets/{id}/schedule
    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<List<DepreciationScheduleDto>>> GetSchedule(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();
        var schedules = await fixedAssets.GetSchedulesAsync(id, branchId, cancellationToken);

        var dtoList = schedules
            .Select(s => mapper.Map<DepreciationScheduleDto>(s))
            .ToList();

        return Ok(dtoList);
    }

    // POST api/fixedassets/{id}/depreciation/post
    [HttpPost("{id:int}/depreciation/post")]
    public async Task<IActionResult> PostDepreciation(
        int id,
        [FromBody] PostDepreciationRequest request,
        CancellationToken cancellationToken)
    {
        await assetService.PostDepreciationForPeriodAsync(
            id,
            request.PeriodStart,
            request.PeriodEnd,
            cancellationToken);

        return NoContent();
    }
}
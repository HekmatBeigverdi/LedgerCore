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
[Route("api/[controller]")]
public class FixedAssetsController(
    IFixedAssetRepository fixedAssets,
    IAssetService assetService,
    IMapper mapper,
    IUnitOfWork uow)
    : ControllerBase
{
    private readonly IUnitOfWork _uow = uow;

    // GET api/fixedassets/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FixedAssetDto>> Get(int id, CancellationToken cancellationToken)
    {
        var asset = await fixedAssets.GetByIdAsync(id, cancellationToken);
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
        var result = await fixedAssets.QueryAsync(paging, cancellationToken);
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
        var existing = await fixedAssets.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing);
        // برای سادگی از همان CreateFixedAssetAsync استفاده نمی‌کنیم، مستقیم آپدیت می‌کنیم
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
        var schedules = await fixedAssets.GetSchedulesAsync(id, cancellationToken);
        var dtoList = schedules.Select(s => mapper.Map<DepreciationScheduleDto>(s)).ToList();
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
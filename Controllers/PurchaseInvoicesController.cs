using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PurchaseInvoicesController(
    IPurchaseService purchaseService,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.Purchase_Invoice_View)]
    public async Task<ActionResult> Query([FromQuery] PagingParams paging, CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var result = await uow.Invoices.QueryPurchaseAsync(branchId, paging, null, cancellationToken);

        var dto = result.Items.Select(mapper.Map<PurchaseInvoiceDto>).ToList();

        return Ok(new
        {
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            Items = dto
        });
    }
    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Purchase_Invoice_View)]
    public async Task<ActionResult<PurchaseInvoiceDto>> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await purchaseService.GetPurchaseInvoiceAsync(id, cancellationToken);
        if (invoice is null)
            return NotFound();

        var dto = mapper.Map<PurchaseInvoiceDto>(invoice);
        return Ok(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Purchase_Invoice_Create)]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create(
        [FromBody] CreatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = mapper.Map<PurchaseInvoice>(request);
        invoice.Lines = request.Lines
            .Select(l => mapper.Map<InvoiceLine>(l))
            .ToList();

        var created = await purchaseService.CreatePurchaseInvoiceAsync(invoice, cancellationToken);

        var branchId = currentBranch.GetRequiredBranchId();
        var dbInvoice = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(created.Id, branchId, cancellationToken) ?? created;

        var dto = mapper.Map<PurchaseInvoiceDto>(dbInvoice);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Purchase_Invoice_Edit)]
    public async Task<ActionResult<PurchaseInvoiceDto>> Update(
        int id,
        [FromBody] UpdatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await purchaseService.GetPurchaseInvoiceAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing);
        existing.Lines.Clear();
        foreach (var lineReq in request.Lines)
        {
            var line = mapper.Map<InvoiceLine>(lineReq);
            existing.Lines.Add(line);
        }

        var updated = await purchaseService.UpdatePurchaseInvoiceAsync(existing, cancellationToken);

        var branchId = currentBranch.GetRequiredBranchId();
        var dbInvoice = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(updated.Id, branchId, cancellationToken) ?? updated;

        var dto = mapper.Map<PurchaseInvoiceDto>(dbInvoice);
        return Ok(dto);
    }

    [HttpPost("{id:int}/post")]
    [HasPermission(PermissionCodes.Purchase_Invoice_Post)]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await purchaseService.PostPurchaseInvoiceAsync(id, cancellationToken);
        return NoContent();
    }
}
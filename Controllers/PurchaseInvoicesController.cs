using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseInvoicesController(
    IPurchaseService purchaseService,
    IUnitOfWork uow,
    IMapper mapper)
    : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await purchaseService.GetPurchaseInvoiceAsync(id, cancellationToken);
        if (invoice is null)
            return NotFound();

        var dto = mapper.Map<PurchaseInvoiceDto>(invoice);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create(
        [FromBody] CreatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = mapper.Map<PurchaseInvoice>(request);
        invoice.Lines = request.Lines
            .Select(l => mapper.Map<InvoiceLine>(l))
            .ToList();

        var created = await purchaseService.CreatePurchaseInvoiceAsync(invoice, cancellationToken);

        var dbInvoice = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(created.Id, cancellationToken)
                       ?? created;

        var dto = mapper.Map<PurchaseInvoiceDto>(dbInvoice);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
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

        var dbInvoice = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(updated.Id, cancellationToken)
                       ?? updated;

        var dto = mapper.Map<PurchaseInvoiceDto>(dbInvoice);
        return Ok(dto);
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await purchaseService.PostPurchaseInvoiceAsync(id, cancellationToken);
        return NoContent();
    }
}
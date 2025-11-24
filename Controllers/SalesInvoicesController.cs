using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SalesInvoicesController(
    ISalesService salesService,
    IUnitOfWork uow,
    IMapper mapper)
    : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalesInvoiceDto>> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await salesService.GetSalesInvoiceAsync(id, cancellationToken);
        if (invoice is null)
            return NotFound();

        // For better display, include navigations (if needed) or re-read from UoW
        var dto = mapper.Map<SalesInvoiceDto>(invoice);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<SalesInvoiceDto>> Create(
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = mapper.Map<SalesInvoice>(request);

        // Map request lines to entity
        invoice.Lines = request.Lines
            .Select(l => mapper.Map<InvoiceLine>(l))
            .ToList();

        var created = await salesService.CreateSalesInvoiceAsync(invoice, cancellationToken);

        // Optionally read again from DB with navigations
        var dbInvoice = await uow.Invoices.GetSalesInvoiceWithLinesAsync(created.Id, cancellationToken)
                       ?? created;

        var dto = mapper.Map<SalesInvoiceDto>(dbInvoice);

        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SalesInvoiceDto>> Update(
        int id,
        [FromBody] UpdateSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await salesService.GetSalesInvoiceAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        mapper.Map(request, existing); // header

        // Build lines from Request (in this simple version, rebuild)
        existing.Lines.Clear();
        foreach (var lineReq in request.Lines)
        {
            var line = mapper.Map<InvoiceLine>(lineReq);
            existing.Lines.Add(line);
        }

        var updated = await salesService.UpdateSalesInvoiceAsync(existing, cancellationToken);

        var dbInvoice = await uow.Invoices.GetSalesInvoiceWithLinesAsync(updated.Id, cancellationToken)
                       ?? updated;

        var dto = mapper.Map<SalesInvoiceDto>(dbInvoice);
        return Ok(dto);
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await salesService.PostSalesInvoiceAsync(id, cancellationToken);
        return NoContent();
    }
}

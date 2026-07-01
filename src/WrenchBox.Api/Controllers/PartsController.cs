using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.Parts;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/parts")]
[Authorize(Roles = "Admin")]
public class PartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PartsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPartsQuery(page, pageSize, activeOnly), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePartRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreatePartCommand(request.Name, request.Sku, request.UnitPrice, request.StockQuantity, request.MinimumStock), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdatePartCommand(id, request.Name, request.UnitPrice, request.MinimumStock, request.IsActive), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/stock")]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AdjustPartStockCommand(id, request.Quantity, request.Reason), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePartCommand(id), cancellationToken);
        return NoContent();
    }

    public record CreatePartRequest(string Name, string Sku, decimal UnitPrice, int StockQuantity, int MinimumStock);
    public record UpdatePartRequest(string Name, decimal UnitPrice, int MinimumStock, bool IsActive);
    public record AdjustStockRequest(int Quantity, string Reason);
}

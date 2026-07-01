using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.Services;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
[Authorize(Roles = "Admin")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetServicesQuery(page, pageSize, activeOnly), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateServiceCommand(request.Name, request.Description, request.UnitPrice, request.EstimatedDurationMinutes), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateServiceCommand(id, request.Name, request.Description, request.UnitPrice, request.EstimatedDurationMinutes, request.IsActive), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteServiceCommand(id), cancellationToken);
        return NoContent();
    }

    public record CreateServiceRequest(string Name, string Description, decimal UnitPrice, int EstimatedDurationMinutes);
    public record UpdateServiceRequest(string Name, string Description, decimal UnitPrice, int EstimatedDurationMinutes, bool IsActive);
}

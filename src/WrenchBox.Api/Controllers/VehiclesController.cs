using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.Vehicles;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize(Roles = "Admin")]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehiclesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetVehiclesQuery(customerId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVehicleByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateVehicleCommand(request.CustomerId, request.Plate, request.Brand, request.Model, request.Year), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateVehicleCommand(id, request.Brand, request.Model, request.Year), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteVehicleCommand(id), cancellationToken);
        return NoContent();
    }

    public record CreateVehicleRequest(Guid CustomerId, string Plate, string Brand, string Model, int Year);
    public record UpdateVehicleRequest(string Brand, string Model, int Year);
}

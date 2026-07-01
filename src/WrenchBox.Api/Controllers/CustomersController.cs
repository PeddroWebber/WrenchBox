using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.Customers;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = "Admin")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomersQuery(page, pageSize, search), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCustomerCommand(request.Document, request.Name, request.Email, request.Phone), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateCustomerCommand(id, request.Name, request.Email, request.Phone), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        return NoContent();
    }

    public record CreateCustomerRequest(string Document, string Name, string Email, string Phone);
    public record UpdateCustomerRequest(string Name, string Email, string Phone);
}

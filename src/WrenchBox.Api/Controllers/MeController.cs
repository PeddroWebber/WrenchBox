using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Me;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/me/work-orders")]
[Authorize(Roles = "Customer")]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetMyWorkOrdersQuery(CustomerId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyWorkOrderStatusQuery(CustomerId, id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<IActionResult> Decision(Guid id, [FromBody] BudgetDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DecideMyBudgetCommand(CustomerId, id, request.Approved),
            cancellationToken);
        return Ok(result);
    }

    private Guid CustomerId
    {
        get
        {
            var value = User.FindFirst("customer_id")?.Value;
            if (!Guid.TryParse(value, out var id))
                throw new UnauthorizedApplicationException("Customer token is missing customer_id.");
            return id;
        }
    }

    public record BudgetDecisionRequest(bool Approved);
}

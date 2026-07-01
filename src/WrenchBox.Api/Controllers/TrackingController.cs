using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.Tracking;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/tracking/work-orders")]
[AllowAnonymous]
public class TrackingController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrackingController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetWorkOrder(CancellationToken cancellationToken)
    {
        var token = Request.Headers["X-Tracking-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "X-Tracking-Token header is required." });

        var result = await _mediator.Send(new GetTrackingWorkOrderQuery(token), cancellationToken);
        return Ok(result);
    }

    [HttpPost("approve")]
    public async Task<IActionResult> ApproveBudget(CancellationToken cancellationToken)
    {
        var token = Request.Headers["X-Tracking-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "X-Tracking-Token header is required." });

        var result = await _mediator.Send(new ApproveBudgetCommand(token), cancellationToken);
        return Ok(result);
    }
}

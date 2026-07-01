using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.Metrics;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/metrics")]
[Authorize(Roles = "Admin")]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MetricsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("average-execution-time")]
    public async Task<IActionResult> GetAverageExecutionTime(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAverageExecutionTimeQuery(), cancellationToken);
        return Ok(result);
    }
}

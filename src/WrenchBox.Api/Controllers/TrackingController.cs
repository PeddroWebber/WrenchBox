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
        var token = ReadTrackingToken();
        if (token is null)
            return BadRequest(new { message = "X-Tracking-Token header is required." });

        var result = await _mediator.Send(new GetTrackingWorkOrderQuery(token), cancellationToken);
        return Ok(result);
    }

    [HttpPost("approve")]
    public async Task<IActionResult> ApproveBudget(CancellationToken cancellationToken)
    {
        var token = ReadTrackingToken();
        if (token is null)
            return BadRequest(new { message = "X-Tracking-Token header is required." });

        var result = await _mediator.Send(new ApproveBudgetCommand(token), cancellationToken);
        return Ok(result);
    }

    [HttpPost("reject")]
    public async Task<IActionResult> RejectBudget(CancellationToken cancellationToken)
    {
        var token = ReadTrackingToken();
        if (token is null)
            return BadRequest(new { message = "X-Tracking-Token header is required." });

        var result = await _mediator.Send(new RejectBudgetCommand(token), cancellationToken);
        return Ok(result);
    }

    [HttpPost("decision")]
    public async Task<IActionResult> DecideBudget(
        [FromBody] BudgetDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var token = ReadTrackingToken();
        if (token is null)
            return BadRequest(new { message = "X-Tracking-Token header is required." });

        var result = await _mediator.Send(new DecideBudgetCommand(token, request.Approved), cancellationToken);
        return Ok(result);
    }

    [HttpGet("decision")]
    public async Task<IActionResult> DecideBudgetFromEmail(
        [FromQuery] bool approved,
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var trackingToken = token ?? ReadTrackingToken();
        if (string.IsNullOrWhiteSpace(trackingToken))
            return BadRequest(new { message = "token query parameter or X-Tracking-Token header is required." });

        var result = await _mediator.Send(new DecideBudgetCommand(trackingToken, approved), cancellationToken);
        var label = approved ? "aprovado" : "recusado";
        var html = $"""
            <html><body style="font-family:sans-serif;padding:2rem">
            <h1>Orçamento {label}</h1>
            <p>OS <strong>{result.OrderNumber}</strong> agora está em <strong>{result.StatusLabel}</strong>.</p>
            </body></html>
            """;

        return Content(html, "text/html");
    }

    private string? ReadTrackingToken()
    {
        var token = Request.Headers["X-Tracking-Token"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public record BudgetDecisionRequest(bool Approved);
}

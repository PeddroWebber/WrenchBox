using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WrenchBox.Application.Features.WorkOrders;
using WrenchBox.Infrastructure.Notifications;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly WebhookSettings _settings;

    public WebhooksController(IMediator mediator, IOptions<WebhookSettings> settings)
    {
        _mediator = mediator;
        _settings = settings.Value;
    }

    [HttpPost("work-order-status")]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] WorkOrderStatusWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var provided = Request.Headers["X-Webhook-Secret"].FirstOrDefault();
        if (!SecretsEqual(provided, _settings.Secret))
            return Unauthorized(new { message = "Invalid webhook secret." });

        if (request.WorkOrderId == Guid.Empty || string.IsNullOrWhiteSpace(request.Action))
            return BadRequest(new { message = "workOrderId and action are required." });

        var result = await _mediator.Send(
            new UpdateWorkOrderStatusFromWebhookCommand(request.WorkOrderId, request.Action),
            cancellationToken);

        return Ok(result);
    }

    private static bool SecretsEqual(string? provided, string expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
            return false;

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
               && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    public record WorkOrderStatusWebhookRequest(Guid WorkOrderId, string Action);
}

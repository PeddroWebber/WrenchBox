using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Features.WorkOrders;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/work-orders")]
[Authorize(Roles = "Admin")]
public class WorkOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WorkOrderStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] bool includeClosed = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetWorkOrdersQuery(page, pageSize, status, customerId, includeClosed),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkOrderStatusQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateWorkOrderCommand(
            request.CustomerDocument,
            request.CustomerName,
            request.CustomerEmail,
            request.CustomerPhone,
            request.VehiclePlate,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleYear,
            request.Services,
            request.Parts,
            request.Notes);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/start-diagnosis")]
    public async Task<IActionResult> StartDiagnosis(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartDiagnosisCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/send-budget")]
    public async Task<IActionResult> SendBudget(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendBudgetCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteWorkOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/deliver")]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeliverWorkOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    public record CreateWorkOrderRequest(
        string CustomerDocument,
        string CustomerName,
        string CustomerEmail,
        string CustomerPhone,
        string VehiclePlate,
        string VehicleBrand,
        string VehicleModel,
        int VehicleYear,
        IReadOnlyList<WorkOrderServiceRequest> Services,
        IReadOnlyList<WorkOrderPartRequest> Parts,
        string? Notes);
}

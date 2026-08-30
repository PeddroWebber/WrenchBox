using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Tracking;

public record GetTrackingWorkOrderQuery(string TrackingToken) : IRequest<TrackingWorkOrderDto>;
public record ApproveBudgetCommand(string TrackingToken) : IRequest<TrackingWorkOrderDto>;
public record RejectBudgetCommand(string TrackingToken) : IRequest<TrackingWorkOrderDto>;
public record DecideBudgetCommand(string TrackingToken, bool Approved) : IRequest<TrackingWorkOrderDto>;

public class GetTrackingWorkOrderQueryHandler : IRequestHandler<GetTrackingWorkOrderQuery, TrackingWorkOrderDto>
{
    private readonly IWorkOrderRepository _repository;

    public GetTrackingWorkOrderQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<TrackingWorkOrderDto> Handle(GetTrackingWorkOrderQuery request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByTrackingTokenAsync(request.TrackingToken, cancellationToken)
            ?? throw new NotFoundException("Work order not found for the provided tracking token.");

        return workOrder.ToTrackingDto();
    }
}

public class ApproveBudgetCommandHandler : IRequestHandler<ApproveBudgetCommand, TrackingWorkOrderDto>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IPartRepository _partRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService? _notificationService;

    public ApproveBudgetCommandHandler(
        IWorkOrderRepository workOrderRepository,
        IPartRepository partRepository,
        IUnitOfWork unitOfWork,
        INotificationService? notificationService = null)
    {
        _workOrderRepository = workOrderRepository;
        _partRepository = partRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<TrackingWorkOrderDto> Handle(ApproveBudgetCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _workOrderRepository.GetByTrackingTokenForUpdateAsync(request.TrackingToken, cancellationToken)
            ?? throw new NotFoundException("Work order not found for the provided tracking token.");

        var partIds = workOrder.PartItems.Select(p => p.PartId).ToList();
        var parts = partIds.Count > 0
            ? await _partRepository.GetByIdsForUpdateAsync(partIds, cancellationToken)
            : [];

        var partsById = parts.ToDictionary(p => p.Id);
        workOrder.ApproveBudget(partsById, "Customer");

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        workOrder = await _workOrderRepository.GetByTrackingTokenAsync(request.TrackingToken, cancellationToken)
            ?? throw new NotFoundException("Work order not found for the provided tracking token.");

        await NotifyStatusAsync(workOrder, cancellationToken);
        return workOrder.ToTrackingDto();
    }

    private async Task NotifyStatusAsync(Domain.Entities.WorkOrder workOrder, CancellationToken cancellationToken)
    {
        if (_notificationService is null || string.IsNullOrWhiteSpace(workOrder.Customer?.Email))
            return;

        await _notificationService.SendStatusChangedAsync(
            workOrder.Customer.Email,
            workOrder.OrderNumber,
            workOrder.Status,
            workOrder.Status.ToPortuguese(),
            cancellationToken);
    }
}

public class RejectBudgetCommandHandler : IRequestHandler<RejectBudgetCommand, TrackingWorkOrderDto>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService? _notificationService;

    public RejectBudgetCommandHandler(
        IWorkOrderRepository workOrderRepository,
        IUnitOfWork unitOfWork,
        INotificationService? notificationService = null)
    {
        _workOrderRepository = workOrderRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<TrackingWorkOrderDto> Handle(RejectBudgetCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _workOrderRepository.GetByTrackingTokenForUpdateAsync(request.TrackingToken, cancellationToken)
            ?? throw new NotFoundException("Work order not found for the provided tracking token.");

        workOrder.RejectBudget("Customer");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        workOrder = await _workOrderRepository.GetByIdAsync(workOrder.Id, cancellationToken)
            ?? throw new NotFoundException("Work order not found after budget rejection.");

        if (_notificationService is not null && !string.IsNullOrWhiteSpace(workOrder.Customer?.Email))
        {
            await _notificationService.SendStatusChangedAsync(
                workOrder.Customer.Email,
                workOrder.OrderNumber,
                workOrder.Status,
                workOrder.Status.ToPortuguese(),
                cancellationToken);
        }

        return workOrder.ToTrackingDto();
    }
}

public class DecideBudgetCommandHandler : IRequestHandler<DecideBudgetCommand, TrackingWorkOrderDto>
{
    private readonly IMediator _mediator;

    public DecideBudgetCommandHandler(IMediator mediator) => _mediator = mediator;

    public Task<TrackingWorkOrderDto> Handle(DecideBudgetCommand request, CancellationToken cancellationToken) =>
        request.Approved
            ? _mediator.Send(new ApproveBudgetCommand(request.TrackingToken), cancellationToken)
            : _mediator.Send(new RejectBudgetCommand(request.TrackingToken), cancellationToken);
}

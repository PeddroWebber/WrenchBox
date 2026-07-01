using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Tracking;

public record GetTrackingWorkOrderQuery(string TrackingToken) : IRequest<TrackingWorkOrderDto>;
public record ApproveBudgetCommand(string TrackingToken) : IRequest<TrackingWorkOrderDto>;

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

    public ApproveBudgetCommandHandler(
        IWorkOrderRepository workOrderRepository,
        IPartRepository partRepository,
        IUnitOfWork unitOfWork)
    {
        _workOrderRepository = workOrderRepository;
        _partRepository = partRepository;
        _unitOfWork = unitOfWork;
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
        return workOrder.ToTrackingDto();
    }
}

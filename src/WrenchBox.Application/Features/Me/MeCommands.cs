using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Me;

public record GetMyWorkOrdersQuery(Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<WorkOrderDto>>;
public record GetMyWorkOrderStatusQuery(Guid CustomerId, Guid WorkOrderId) : IRequest<WorkOrderStatusDto>;
public record DecideMyBudgetCommand(Guid CustomerId, Guid WorkOrderId, bool Approved) : IRequest<WorkOrderStatusDto>;

public class GetMyWorkOrdersQueryHandler : IRequestHandler<GetMyWorkOrdersQuery, PagedResult<WorkOrderDto>>
{
    private readonly IWorkOrderRepository _repository;

    public GetMyWorkOrdersQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<PagedResult<WorkOrderDto>> Handle(GetMyWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            status: null,
            customerId: request.CustomerId,
            includeClosed: true,
            cancellationToken);

        return new PagedResult<WorkOrderDto>
        {
            Items = items.Select(w => w.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

public class GetMyWorkOrderStatusQueryHandler : IRequestHandler<GetMyWorkOrderStatusQuery, WorkOrderStatusDto>
{
    private readonly IWorkOrderRepository _repository;

    public GetMyWorkOrderStatusQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<WorkOrderStatusDto> Handle(GetMyWorkOrderStatusQuery request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        if (workOrder.CustomerId != request.CustomerId)
            throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        return workOrder.ToStatusDto();
    }
}

public class DecideMyBudgetCommandHandler : IRequestHandler<DecideMyBudgetCommand, WorkOrderStatusDto>
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IPartRepository _partRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService? _notificationService;

    public DecideMyBudgetCommandHandler(
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

    public async Task<WorkOrderStatusDto> Handle(DecideMyBudgetCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _workOrderRepository.GetByIdWithPartsForUpdateAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        if (workOrder.CustomerId != request.CustomerId)
            throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        if (request.Approved)
        {
            var partIds = workOrder.PartItems.Select(p => p.PartId).ToList();
            var parts = partIds.Count > 0
                ? await _partRepository.GetByIdsForUpdateAsync(partIds, cancellationToken)
                : [];
            workOrder.ApproveBudget(parts.ToDictionary(p => p.Id), "Customer");
        }
        else
        {
            workOrder.RejectBudget("Customer");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        if (_notificationService is not null && !string.IsNullOrWhiteSpace(saved.Customer?.Email))
        {
            await _notificationService.SendStatusChangedAsync(
                saved.Customer.Email,
                saved.OrderNumber,
                saved.Status,
                saved.Status.ToPortuguese(),
                cancellationToken);
        }

        return saved.ToStatusDto();
    }
}

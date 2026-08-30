using FluentValidation;
using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Interfaces;
using WrenchBox.Application.Validators;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Application.Features.WorkOrders;

public record WorkOrderServiceRequest(Guid ServiceId, int Quantity);
public record WorkOrderPartRequest(Guid PartId, int Quantity);

public record CreateWorkOrderCommand(
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
    string? Notes) : IRequest<WorkOrderDto>;

public record GetWorkOrderByIdQuery(Guid Id) : IRequest<WorkOrderDto>;
public record GetWorkOrderStatusQuery(Guid Id) : IRequest<WorkOrderStatusDto>;
public record GetWorkOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    WorkOrderStatus? Status = null,
    Guid? CustomerId = null,
    bool IncludeClosed = false) : IRequest<PagedResult<WorkOrderDto>>;
public record StartDiagnosisCommand(Guid WorkOrderId) : IRequest<WorkOrderDto>;
public record SendBudgetCommand(Guid WorkOrderId) : IRequest<SendBudgetResponseDto>;
public record CompleteWorkOrderCommand(Guid WorkOrderId) : IRequest<WorkOrderDto>;
public record DeliverWorkOrderCommand(Guid WorkOrderId) : IRequest<WorkOrderDto>;
public record UpdateWorkOrderStatusFromWebhookCommand(Guid WorkOrderId, string Action) : IRequest<WorkOrderDto>;

public class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.CustomerDocument).ValidDocument();
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.VehiclePlate).ValidPlate();
        RuleFor(x => x.VehicleBrand).NotEmpty();
        RuleFor(x => x.VehicleModel).NotEmpty();
        RuleFor(x => x.Services).NotEmpty();
        RuleForEach(x => x.Services).ChildRules(s =>
        {
            s.RuleFor(x => x.ServiceId).NotEmpty();
            s.RuleFor(x => x.Quantity).GreaterThan(0);
        });
        RuleForEach(x => x.Parts).ChildRules(p =>
        {
            p.RuleFor(x => x.PartId).NotEmpty();
            p.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, WorkOrderDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IPartRepository _partRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkOrderCommandHandler(
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IServiceRepository serviceRepository,
        IPartRepository partRepository,
        IWorkOrderRepository workOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _serviceRepository = serviceRepository;
        _partRepository = partRepository;
        _workOrderRepository = workOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkOrderDto> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var doc = Document.Create(request.CustomerDocument);
        var customer = await _customerRepository.GetByDocumentAsync(doc.Value, cancellationToken);

        if (customer is null)
        {
            customer = Customer.Create(request.CustomerDocument, request.CustomerName, request.CustomerEmail, request.CustomerPhone);
            await _customerRepository.AddAsync(customer, cancellationToken);
        }

        var plate = Plate.Create(request.VehiclePlate);
        var vehicle = await _vehicleRepository.GetByPlateAsync(plate.Value, cancellationToken);

        if (vehicle is null)
        {
            vehicle = customer.AddVehicle(request.VehiclePlate, request.VehicleBrand, request.VehicleModel, request.VehicleYear);
            await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        }
        else if (vehicle.CustomerId != customer.Id)
        {
            throw new AppException("Vehicle plate is registered to another customer.");
        }

        var serviceIds = request.Services.Select(s => s.ServiceId).ToList();
        var services = await _serviceRepository.GetByIdsAsync(serviceIds, cancellationToken);
        if (services.Count != serviceIds.Distinct().Count())
            throw new NotFoundException("One or more services were not found.");

        var serviceTuples = request.Services
            .Select(r =>
            {
                var service = services.First(s => s.Id == r.ServiceId);
                return (service, r.Quantity);
            })
            .ToList();

        var partIds = request.Parts.Select(p => p.PartId).ToList();
        var parts = partIds.Count > 0
            ? await _partRepository.GetByIdsAsync(partIds, cancellationToken)
            : [];

        if (parts.Count != partIds.Distinct().Count())
            throw new NotFoundException("One or more parts were not found.");

        var partTuples = request.Parts
            .Select(r =>
            {
                var part = parts.First(p => p.Id == r.PartId);
                return (part, r.Quantity);
            })
            .ToList();

        var orderNumber = await _workOrderRepository.GenerateOrderNumberAsync(cancellationToken);
        var workOrder = WorkOrder.Create(orderNumber, customer.Id, vehicle.Id, serviceTuples, partTuples, request.Notes);

        await _workOrderRepository.AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        workOrder = await _workOrderRepository.GetByIdAsync(workOrder.Id, cancellationToken)
            ?? throw new AppException("Failed to load created work order.");

        return workOrder.ToDto();
    }
}

public class GetWorkOrderByIdQueryHandler : IRequestHandler<GetWorkOrderByIdQuery, WorkOrderDto>
{
    private readonly IWorkOrderRepository _repository;

    public GetWorkOrderByIdQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<WorkOrderDto> Handle(GetWorkOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.Id}' not found.");
        return workOrder.ToDto();
    }
}

public class GetWorkOrderStatusQueryHandler : IRequestHandler<GetWorkOrderStatusQuery, WorkOrderStatusDto>
{
    private readonly IWorkOrderRepository _repository;

    public GetWorkOrderStatusQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<WorkOrderStatusDto> Handle(GetWorkOrderStatusQuery request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.Id}' not found.");
        return workOrder.ToStatusDto();
    }
}

public class GetWorkOrdersQueryHandler : IRequestHandler<GetWorkOrdersQuery, PagedResult<WorkOrderDto>>
{
    private readonly IWorkOrderRepository _repository;

    public GetWorkOrdersQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<PagedResult<WorkOrderDto>> Handle(GetWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Status,
            request.CustomerId,
            request.IncludeClosed,
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

public class StartDiagnosisCommandHandler : IRequestHandler<StartDiagnosisCommand, WorkOrderDto>
{
    private readonly IWorkOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService? _notificationService;

    public StartDiagnosisCommandHandler(
        IWorkOrderRepository repository,
        IUnitOfWork unitOfWork,
        INotificationService? notificationService = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(StartDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdForUpdateAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        workOrder.StartDiagnosis("Admin");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        workOrder = await _repository.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        await NotifyStatusAsync(workOrder, cancellationToken);
        return workOrder.ToDto();
    }

    private async Task NotifyStatusAsync(WorkOrder workOrder, CancellationToken cancellationToken)
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

public class SendBudgetCommandHandler : IRequestHandler<SendBudgetCommand, SendBudgetResponseDto>
{
    private readonly IWorkOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public SendBudgetCommandHandler(
        IWorkOrderRepository repository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<SendBudgetResponseDto> Handle(SendBudgetCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdForUpdateAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        var token = workOrder.SendBudgetForApproval("Admin");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _repository.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        var customerEmail = saved.Customer?.Email ?? string.Empty;
        var notificationSent = false;
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            notificationSent = await _notificationService.SendBudgetApprovalRequestAsync(
                customerEmail,
                saved.OrderNumber,
                saved.TotalAmount,
                token,
                cancellationToken);
        }

        return new SendBudgetResponseDto(saved.Id, token, notificationSent);
    }
}

public class CompleteWorkOrderCommandHandler : IRequestHandler<CompleteWorkOrderCommand, WorkOrderDto>
{
    private readonly IWorkOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService? _notificationService;

    public CompleteWorkOrderCommandHandler(
        IWorkOrderRepository repository,
        IUnitOfWork unitOfWork,
        INotificationService? notificationService = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(CompleteWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdForUpdateAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        workOrder.Complete("Admin");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        workOrder = await _repository.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        await NotifyStatusAsync(workOrder, cancellationToken);
        return workOrder.ToDto();
    }

    private async Task NotifyStatusAsync(WorkOrder workOrder, CancellationToken cancellationToken)
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

public class DeliverWorkOrderCommandHandler : IRequestHandler<DeliverWorkOrderCommand, WorkOrderDto>
{
    private readonly IWorkOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService? _notificationService;

    public DeliverWorkOrderCommandHandler(
        IWorkOrderRepository repository,
        IUnitOfWork unitOfWork,
        INotificationService? notificationService = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(DeliverWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdForUpdateAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        workOrder.Deliver("Admin");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        workOrder = await _repository.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' not found.");

        await NotifyStatusAsync(workOrder, cancellationToken);
        return workOrder.ToDto();
    }

    private async Task NotifyStatusAsync(WorkOrder workOrder, CancellationToken cancellationToken)
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

public class UpdateWorkOrderStatusFromWebhookCommandHandler
    : IRequestHandler<UpdateWorkOrderStatusFromWebhookCommand, WorkOrderDto>
{
    private readonly IMediator _mediator;

    public UpdateWorkOrderStatusFromWebhookCommandHandler(IMediator mediator) => _mediator = mediator;

    public Task<WorkOrderDto> Handle(UpdateWorkOrderStatusFromWebhookCommand request, CancellationToken cancellationToken)
    {
        var action = request.Action.Trim().ToLowerInvariant();
        return action switch
        {
            "start-diagnosis" or "startdiagnosis" =>
                _mediator.Send(new StartDiagnosisCommand(request.WorkOrderId), cancellationToken),
            "complete" =>
                _mediator.Send(new CompleteWorkOrderCommand(request.WorkOrderId), cancellationToken),
            "deliver" =>
                _mediator.Send(new DeliverWorkOrderCommand(request.WorkOrderId), cancellationToken),
            _ => throw new AppException(
                "Unsupported webhook action. Use start-diagnosis, complete or deliver.")
        };
    }
}

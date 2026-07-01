using FluentValidation;
using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Services;

public record CreateServiceCommand(string Name, string Description, decimal UnitPrice, int EstimatedDurationMinutes) : IRequest<ServiceDto>;
public record UpdateServiceCommand(Guid Id, string Name, string Description, decimal UnitPrice, int EstimatedDurationMinutes, bool IsActive) : IRequest<ServiceDto>;
public record DeleteServiceCommand(Guid Id) : IRequest;
public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto>;
public record GetServicesQuery(int Page = 1, int PageSize = 20, bool? ActiveOnly = null) : IRequest<PagedResult<ServiceDto>>;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0);
    }
}

public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateServiceCommandHandler(IServiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = Service.Create(request.Name, request.Description, request.UnitPrice, request.EstimatedDurationMinutes);
        await _repository.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return service.ToDto();
    }
}

public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceCommandHandler(IServiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Service '{request.Id}' not found.");

        service.Update(request.Name, request.Description, request.UnitPrice, request.EstimatedDurationMinutes, request.IsActive);
        _repository.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return service.ToDto();
    }
}

public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand>
{
    private readonly IServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceCommandHandler(IServiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Service '{request.Id}' not found.");

        _repository.Remove(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ServiceDto>
{
    private readonly IServiceRepository _repository;

    public GetServiceByIdQueryHandler(IServiceRepository repository) => _repository = repository;

    public async Task<ServiceDto> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Service '{request.Id}' not found.");
        return service.ToDto();
    }
}

public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, PagedResult<ServiceDto>>
{
    private readonly IServiceRepository _repository;

    public GetServicesQueryHandler(IServiceRepository repository) => _repository = repository;

    public async Task<PagedResult<ServiceDto>> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.ActiveOnly, cancellationToken);
        return new PagedResult<ServiceDto>
        {
            Items = items.Select(s => s.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

using FluentValidation;
using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Validators;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Vehicles;

public record CreateVehicleCommand(Guid CustomerId, string Plate, string Brand, string Model, int Year) : IRequest<VehicleDto>;
public record UpdateVehicleCommand(Guid Id, string Brand, string Model, int Year) : IRequest<VehicleDto>;
public record DeleteVehicleCommand(Guid Id) : IRequest;
public record GetVehicleByIdQuery(Guid Id) : IRequest<VehicleDto>;
public record GetVehiclesQuery(Guid? CustomerId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<VehicleDto>>;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Plate).ValidPlate();
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVehicleCommandHandler(
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<VehicleDto> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Customer '{request.CustomerId}' not found.");

        var existingPlate = await _vehicleRepository.GetByPlateAsync(request.Plate, cancellationToken);
        if (existingPlate is not null)
            throw new AppException("A vehicle with this plate already exists.");

        var vehicle = customer.AddVehicle(request.Plate, request.Brand, request.Model, request.Year);
        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return vehicle.ToDto();
    }
}

public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, VehicleDto>
{
    private readonly IVehicleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVehicleCommandHandler(IVehicleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<VehicleDto> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Vehicle '{request.Id}' not found.");

        vehicle.Update(request.Brand, request.Model, request.Year);
        _repository.Update(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return vehicle.ToDto();
    }
}

public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand>
{
    private readonly IVehicleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteVehicleCommandHandler(IVehicleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Vehicle '{request.Id}' not found.");

        _repository.Remove(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
{
    private readonly IVehicleRepository _repository;

    public GetVehicleByIdQueryHandler(IVehicleRepository repository) => _repository = repository;

    public async Task<VehicleDto> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Vehicle '{request.Id}' not found.");
        return vehicle.ToDto();
    }
}

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, PagedResult<VehicleDto>>
{
    private readonly IVehicleRepository _repository;

    public GetVehiclesQueryHandler(IVehicleRepository repository) => _repository = repository;

    public async Task<PagedResult<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.CustomerId, request.Page, request.PageSize, cancellationToken);
        return new PagedResult<VehicleDto>
        {
            Items = items.Select(v => v.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

using FluentValidation;
using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Parts;

public record CreatePartCommand(string Name, string Sku, decimal UnitPrice, int StockQuantity, int MinimumStock) : IRequest<PartDto>;
public record UpdatePartCommand(Guid Id, string Name, decimal UnitPrice, int MinimumStock, bool IsActive) : IRequest<PartDto>;
public record AdjustPartStockCommand(Guid Id, int Quantity, string Reason) : IRequest<PartDto>;
public record DeletePartCommand(Guid Id) : IRequest;
public record GetPartByIdQuery(Guid Id) : IRequest<PartDto>;
public record GetPartsQuery(int Page = 1, int PageSize = 20, bool? ActiveOnly = null) : IRequest<PagedResult<PartDto>>;

public class CreatePartCommandValidator : AbstractValidator<CreatePartCommand>
{
    public CreatePartCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
    }
}

public class CreatePartCommandHandler : IRequestHandler<CreatePartCommand, PartDto>
{
    private readonly IPartRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePartCommandHandler(IPartRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PartDto> Handle(CreatePartCommand request, CancellationToken cancellationToken)
    {
        var part = Part.Create(request.Name, request.Sku, request.UnitPrice, request.StockQuantity, request.MinimumStock);
        await _repository.AddAsync(part, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return part.ToDto();
    }
}

public class UpdatePartCommandHandler : IRequestHandler<UpdatePartCommand, PartDto>
{
    private readonly IPartRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePartCommandHandler(IPartRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PartDto> Handle(UpdatePartCommand request, CancellationToken cancellationToken)
    {
        var part = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Part '{request.Id}' not found.");

        part.Update(request.Name, request.UnitPrice, request.MinimumStock, request.IsActive);
        _repository.Update(part);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return part.ToDto();
    }
}

public class AdjustPartStockCommandHandler : IRequestHandler<AdjustPartStockCommand, PartDto>
{
    private readonly IPartRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustPartStockCommandHandler(IPartRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PartDto> Handle(AdjustPartStockCommand request, CancellationToken cancellationToken)
    {
        var part = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Part '{request.Id}' not found.");

        part.AdjustStock(request.Quantity, request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return part.ToDto();
    }
}

public class DeletePartCommandHandler : IRequestHandler<DeletePartCommand>
{
    private readonly IPartRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePartCommandHandler(IPartRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeletePartCommand request, CancellationToken cancellationToken)
    {
        var part = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Part '{request.Id}' not found.");

        _repository.Remove(part);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class GetPartByIdQueryHandler : IRequestHandler<GetPartByIdQuery, PartDto>
{
    private readonly IPartRepository _repository;

    public GetPartByIdQueryHandler(IPartRepository repository) => _repository = repository;

    public async Task<PartDto> Handle(GetPartByIdQuery request, CancellationToken cancellationToken)
    {
        var part = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Part '{request.Id}' not found.");
        return part.ToDto();
    }
}

public class GetPartsQueryHandler : IRequestHandler<GetPartsQuery, PagedResult<PartDto>>
{
    private readonly IPartRepository _repository;

    public GetPartsQueryHandler(IPartRepository repository) => _repository = repository;

    public async Task<PagedResult<PartDto>> Handle(GetPartsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.ActiveOnly, cancellationToken);
        return new PagedResult<PartDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

using FluentValidation;
using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Validators;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Application.Features.Customers;

public record CreateCustomerCommand(string Document, string Name, string Email, string Phone) : IRequest<CustomerDto>;
public record UpdateCustomerCommand(Guid Id, string Name, string Email, string Phone) : IRequest<CustomerDto>;
public record DeleteCustomerCommand(Guid Id) : IRequest;
public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;
public record GetCustomersQuery(int Page = 1, int PageSize = 20, string? Search = null) : IRequest<PagedResult<CustomerDto>>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Document).ValidDocument();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var doc = Document.Create(request.Document);
        var existing = await _repository.GetByDocumentAsync(doc.Value, cancellationToken);
        if (existing is not null)
            throw new AppException("A customer with this document already exists.");

        var customer = Customer.Create(request.Document, request.Name, request.Email, request.Phone);
        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{request.Id}' not found.");

        customer.Update(request.Name, request.Email, request.Phone);
        _repository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }
}

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerCommandHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{request.Id}' not found.");

        _repository.Remove(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdQueryHandler(ICustomerRepository repository) => _repository = repository;

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{request.Id}' not found.");
        return customer.ToDto();
    }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _repository;

    public GetCustomersQueryHandler(ICustomerRepository repository) => _repository = repository;

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.Search, cancellationToken);
        return new PagedResult<CustomerDto>
        {
            Items = items.Select(c => c.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

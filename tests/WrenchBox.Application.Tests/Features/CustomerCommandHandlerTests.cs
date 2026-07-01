using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Customers;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class CustomerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Create_Success_ReturnsDto()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new CreateCustomerCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new CreateCustomerCommand("39053344705", "João", "joao@test.com", "11999999999"),
            CancellationToken.None);

        result.Name.Should().Be("João");
        repo.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateDocument_Throws()
    {
        var existing = Customer.Create("39053344705", "Existing", "e@test.com", "119");
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByDocumentAsync("39053344705", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = new CreateCustomerCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new CreateCustomerCommand("39053344705", "João", "joao@test.com", "11999999999"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task Update_Success_ReturnsDto()
    {
        var customer = Customer.Create("39053344705", "João", "joao@test.com", "11999999999");
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var handler = new UpdateCustomerCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new UpdateCustomerCommand(customer.Id, "João Updated", "updated@test.com", "11888888888"),
            CancellationToken.None);

        result.Name.Should().Be("João Updated");
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new UpdateCustomerCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new UpdateCustomerCommand(Guid.NewGuid(), "X", "x@test.com", "119"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_Success()
    {
        var customer = Customer.Create("39053344705", "João", "joao@test.com", "11999999999");
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var handler = new DeleteCustomerCommandHandler(repo.Object, _unitOfWork.Object);
        await handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

        repo.Verify(r => r.Remove(customer), Times.Once);
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new DeleteCustomerCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(new DeleteCustomerCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCustomers_ReturnsPagedResult()
    {
        var customers = new List<Customer> { Customer.Create("39053344705", "João", "j@test.com", "119") };
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((customers, 1));

        var handler = new GetCustomersQueryHandler(repo.Object);
        var result = await handler.Handle(new GetCustomersQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }
}

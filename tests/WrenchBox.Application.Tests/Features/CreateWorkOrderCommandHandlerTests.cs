using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.WorkOrders;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class CreateWorkOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesCustomer_WhenNotExists()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        var vehicleRepo = new Mock<IVehicleRepository>();
        var serviceRepo = new Mock<IServiceRepository>();
        var partRepo = new Mock<IPartRepository>();
        var workOrderRepo = new Mock<IWorkOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        customerRepo.Setup(r => r.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        vehicleRepo.Setup(r => r.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var service = Service.Create("Oil Change", "Desc", 100m, 60);
        serviceRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { service });
        partRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Part>());

        workOrderRepo.Setup(r => r.GenerateOrderNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("WO-2026-00001");

        Customer? capturedCustomer = null;
        customerRepo.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => capturedCustomer = c)
            .Returns(Task.CompletedTask);

        WorkOrder? capturedOrder = null;
        workOrderRepo.Setup(r => r.AddAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()))
            .Callback<WorkOrder, CancellationToken>((w, _) => capturedOrder = w)
            .Returns(Task.CompletedTask);

        workOrderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedOrder);

        var handler = new CreateWorkOrderCommandHandler(
            customerRepo.Object,
            vehicleRepo.Object,
            serviceRepo.Object,
            partRepo.Object,
            workOrderRepo.Object,
            unitOfWork.Object);

        var command = new CreateWorkOrderCommand(
            "39053344705",
            "John Doe",
            "john@example.com",
            "11999999999",
            "ABC1D23",
            "Toyota",
            "Corolla",
            2020,
            [new WorkOrderServiceRequest(service.Id, 1)],
            [],
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        capturedCustomer.Should().NotBeNull();
        capturedCustomer!.Name.Should().Be("John Doe");
        result.TotalAmount.Should().Be(100m);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingService_ThrowsNotFound()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        var vehicleRepo = new Mock<IVehicleRepository>();
        var serviceRepo = new Mock<IServiceRepository>();
        var partRepo = new Mock<IPartRepository>();
        var workOrderRepo = new Mock<IWorkOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var customer = Customer.Create("39053344705", "John", "john@example.com", "11999999999");
        customerRepo.Setup(r => r.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        vehicleRepo.Setup(r => r.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);
        serviceRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service>());

        var handler = new CreateWorkOrderCommandHandler(
            customerRepo.Object,
            vehicleRepo.Object,
            serviceRepo.Object,
            partRepo.Object,
            workOrderRepo.Object,
            unitOfWork.Object);

        var command = new CreateWorkOrderCommand(
            "39053344705", "John", "john@example.com", "11999999999",
            "ABC1D23", "Toyota", "Corolla", 2020,
            [new WorkOrderServiceRequest(Guid.NewGuid(), 1)], [], null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

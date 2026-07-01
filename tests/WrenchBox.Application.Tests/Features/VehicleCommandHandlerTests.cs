using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Vehicles;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class VehicleCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Create_Success_ReturnsDto()
    {
        var customer = Customer.Create("39053344705", "João", "j@test.com", "119");
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var vehicleRepo = new Mock<IVehicleRepository>();
        vehicleRepo.Setup(r => r.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var handler = new CreateVehicleCommandHandler(customerRepo.Object, vehicleRepo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new CreateVehicleCommand(customer.Id, "ABC1D23", "Toyota", "Corolla", 2020),
            CancellationToken.None);

        result.Plate.Should().Be("ABC1D23");
        vehicleRepo.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CustomerNotFound_Throws()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new CreateVehicleCommandHandler(
            customerRepo.Object,
            new Mock<IVehicleRepository>().Object,
            _unitOfWork.Object);

        var act = async () => await handler.Handle(
            new CreateVehicleCommand(Guid.NewGuid(), "ABC1D23", "Toyota", "Corolla", 2020),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_DuplicatePlate_Throws()
    {
        var customer = Customer.Create("39053344705", "João", "j@test.com", "119");
        var existing = customer.AddVehicle("ABC1D23", "Honda", "Civic", 2019);

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var vehicleRepo = new Mock<IVehicleRepository>();
        vehicleRepo.Setup(r => r.GetByPlateAsync("ABC1D23", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = new CreateVehicleCommandHandler(customerRepo.Object, vehicleRepo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new CreateVehicleCommand(customer.Id, "ABC1D23", "Toyota", "Corolla", 2020),
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task Update_Success_ReturnsDto()
    {
        var customer = Customer.Create("39053344705", "João", "j@test.com", "119");
        var vehicle = customer.AddVehicle("ABC1D23", "Toyota", "Corolla", 2020);

        var repo = new Mock<IVehicleRepository>();
        repo.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var handler = new UpdateVehicleCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new UpdateVehicleCommand(vehicle.Id, "Toyota", "Corolla XEi", 2021),
            CancellationToken.None);

        result.Model.Should().Be("Corolla XEi");
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var repo = new Mock<IVehicleRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var handler = new UpdateVehicleCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new UpdateVehicleCommand(Guid.NewGuid(), "Toyota", "Corolla", 2020),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_Success()
    {
        var customer = Customer.Create("39053344705", "João", "j@test.com", "119");
        var vehicle = customer.AddVehicle("ABC1D23", "Toyota", "Corolla", 2020);

        var repo = new Mock<IVehicleRepository>();
        repo.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var handler = new DeleteVehicleCommandHandler(repo.Object, _unitOfWork.Object);
        await handler.Handle(new DeleteVehicleCommand(vehicle.Id), CancellationToken.None);

        repo.Verify(r => r.Remove(vehicle), Times.Once);
    }

    [Fact]
    public async Task GetById_Success_ReturnsDto()
    {
        var customer = Customer.Create("39053344705", "João", "j@test.com", "119");
        var vehicle = customer.AddVehicle("ABC1D23", "Toyota", "Corolla", 2020);

        var repo = new Mock<IVehicleRepository>();
        repo.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var handler = new GetVehicleByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetVehicleByIdQuery(vehicle.Id), CancellationToken.None);

        result.Brand.Should().Be("Toyota");
    }

    [Fact]
    public async Task GetVehicles_ReturnsPagedResult()
    {
        var customer = Customer.Create("39053344705", "João", "j@test.com", "119");
        var vehicle = customer.AddVehicle("ABC1D23", "Toyota", "Corolla", 2020);

        var repo = new Mock<IVehicleRepository>();
        repo.Setup(r => r.GetPagedAsync(null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle> { vehicle }, 1));

        var handler = new GetVehiclesQueryHandler(repo.Object);
        var result = await handler.Handle(new GetVehiclesQuery(null), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }
}

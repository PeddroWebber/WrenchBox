using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Services;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class ServiceCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Create_Success_ReturnsDto()
    {
        var repo = new Mock<IServiceRepository>();
        var handler = new CreateServiceCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new CreateServiceCommand("Troca de Óleo", "Oil change", 150m, 30),
            CancellationToken.None);

        result.Name.Should().Be("Troca de Óleo");
        repo.Verify(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_Success_ReturnsDto()
    {
        var service = Service.Create("Troca de Óleo", "Oil change", 150m, 30);
        var repo = new Mock<IServiceRepository>();
        repo.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>())).ReturnsAsync(service);

        var handler = new UpdateServiceCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new UpdateServiceCommand(service.Id, "Troca de Óleo Premium", "Updated", 180m, 45, true),
            CancellationToken.None);

        result.UnitPrice.Should().Be(180m);
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var repo = new Mock<IServiceRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        var handler = new UpdateServiceCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new UpdateServiceCommand(Guid.NewGuid(), "X", "D", 10m, 30, true),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_Success()
    {
        var service = Service.Create("Troca de Óleo", "Oil change", 150m, 30);
        var repo = new Mock<IServiceRepository>();
        repo.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>())).ReturnsAsync(service);

        var handler = new DeleteServiceCommandHandler(repo.Object, _unitOfWork.Object);
        await handler.Handle(new DeleteServiceCommand(service.Id), CancellationToken.None);

        repo.Verify(r => r.Remove(service), Times.Once);
    }

    [Fact]
    public async Task GetById_Success_ReturnsDto()
    {
        var service = Service.Create("Troca de Óleo", "Oil change", 150m, 30);
        var repo = new Mock<IServiceRepository>();
        repo.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>())).ReturnsAsync(service);

        var handler = new GetServiceByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetServiceByIdQuery(service.Id), CancellationToken.None);

        result.Name.Should().Be("Troca de Óleo");
    }

    [Fact]
    public async Task GetServices_ReturnsPagedResult()
    {
        var service = Service.Create("Troca de Óleo", "Oil change", 150m, 30);
        var repo = new Mock<IServiceRepository>();
        repo.Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Service> { service }, 1));

        var handler = new GetServicesQueryHandler(repo.Object);
        var result = await handler.Handle(new GetServicesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }
}

using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Tracking;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class ApproveBudgetCommandHandlerTests
{
    [Fact]
    public async Task Handle_InsufficientStock_ThrowsDomainException()
    {
        var part = Part.Create("Filter", "FLT-1", 20m, 2, 1);
        var service = Service.Create("Oil", "Desc", 50m, 30);
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(),
            [(service, 1)], [(part, 1)]);
        order.StartDiagnosis();
        order.SendBudgetForApproval();
        part.Deduct(2, null, "External sale");

        var workOrderRepo = new Mock<IWorkOrderRepository>();
        workOrderRepo.Setup(r => r.GetByTrackingTokenForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var partRepo = new Mock<IPartRepository>();
        partRepo.Setup(r => r.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Part> { part });

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ApproveBudgetCommandHandler(workOrderRepo.Object, partRepo.Object, unitOfWork.Object);

        var act = () => handler.Handle(new ApproveBudgetCommand(order.TrackingToken!), CancellationToken.None);
        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task Handle_ValidApproval_UpdatesStatus()
    {
        var part = Part.Create("Filter", "FLT-1", 20m, 10, 1);
        var service = Service.Create("Oil", "Desc", 50m, 30);
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(),
            [(service, 1)], [(part, 2)]);
        order.StartDiagnosis();
        order.SendBudgetForApproval();

        var workOrderRepo = new Mock<IWorkOrderRepository>();
        workOrderRepo.Setup(r => r.GetByTrackingTokenForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        workOrderRepo.Setup(r => r.GetByTrackingTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var partRepo = new Mock<IPartRepository>();
        partRepo.Setup(r => r.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Part> { part });

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ApproveBudgetCommandHandler(workOrderRepo.Object, partRepo.Object, unitOfWork.Object);

        var result = await handler.Handle(new ApproveBudgetCommand(order.TrackingToken!), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.InExecution);
        part.StockQuantity.Should().Be(8);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsNotFound()
    {
        var workOrderRepo = new Mock<IWorkOrderRepository>();
        workOrderRepo.Setup(r => r.GetByTrackingTokenForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new ApproveBudgetCommandHandler(
            workOrderRepo.Object,
            new Mock<IPartRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => handler.Handle(new ApproveBudgetCommand("invalid"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Tracking;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class TrackingQueryHandlerTests
{
    [Fact]
    public async Task GetTrackingWorkOrder_Success()
    {
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(), [(service, 1)], []);
        order.StartDiagnosis();
        var token = order.SendBudgetForApproval();

        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByTrackingTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new GetTrackingWorkOrderQueryHandler(repo.Object);
        var result = await handler.Handle(new GetTrackingWorkOrderQuery(token), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        result.OrderNumber.Should().Be("WO-1");
    }

    [Fact]
    public async Task GetTrackingWorkOrder_NotFound_Throws()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByTrackingTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new GetTrackingWorkOrderQueryHandler(repo.Object);
        var act = async () => await handler.Handle(new GetTrackingWorkOrderQuery("invalid"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

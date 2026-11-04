using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Me;
using WrenchBox.Application.Features.Metrics;
using WrenchBox.Application.Notifications;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class MeAndNotificationTests
{
    [Fact]
    public async Task GetMyWorkOrderStatus_OtherCustomer_ThrowsNotFound()
    {
        var owner = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-1", owner, Guid.NewGuid(), [(service, 1)], []);

        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new GetMyWorkOrderStatusQueryHandler(repo.Object);
        var act = async () => await handler.Handle(new GetMyWorkOrderStatusQuery(stranger, order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMyWorkOrderStatus_Owner_ReturnsStatus()
    {
        var owner = Guid.NewGuid();
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-9", owner, Guid.NewGuid(), [(service, 1)], []);

        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new GetMyWorkOrderStatusQueryHandler(repo.Object);
        var result = await handler.Handle(new GetMyWorkOrderStatusQuery(owner, order.Id), CancellationToken.None);

        result.OrderNumber.Should().Be("WO-9");
    }

    [Theory]
    [InlineData(null, NotificationChannel.Smtp)]
    [InlineData("", NotificationChannel.Smtp)]
    [InlineData("   ", NotificationChannel.Smtp)]
    [InlineData("http://function:80", NotificationChannel.Function)]
    public void NotificationRoute_ChoosesChannel(string? baseUrl, NotificationChannel expected)
    {
        NotificationRoute.Choose(baseUrl).Should().Be(expected);
    }
}

public class OperationalMetricsQueryTests
{
    [Fact]
    public async Task Handle_ComputesDailyVolumePhaseAveragesAndFailures()
    {
        var day = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var timings = new List<WorkOrderTiming>
        {
            new(day.AddHours(2), day.AddHours(1), day.AddHours(2), null, null, null),
            new(day.AddDays(-1), day.AddDays(-1), day.AddDays(-1).AddMinutes(30), day.AddDays(-1), day.AddDays(-1).AddMinutes(20), day.AddDays(-1).AddMinutes(25))
        };

        var orders = new Mock<IWorkOrderRepository>();
        orders.Setup(r => r.ListTimingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(timings);
        var failures = new Mock<IIntegrationFailureRepository>();
        failures.Setup(r => r.CountSinceAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var handler = new GetOperationalMetricsQueryHandler(orders.Object, failures.Object);
        var result = await handler.Handle(new GetOperationalMetricsQuery(day), CancellationToken.None);

        result.OpenedToday.Should().Be(1);
        result.DiagnosisSampleCount.Should().Be(2);
        result.ExecutionSampleCount.Should().Be(1);
        result.FinalizationSampleCount.Should().Be(1);
        result.ExecutionSampleCount.Should().Be(1);
        result.AverageExecutionMinutes.Should().Be(20);
        result.IntegrationFailuresToday.Should().Be(2);
    }
}

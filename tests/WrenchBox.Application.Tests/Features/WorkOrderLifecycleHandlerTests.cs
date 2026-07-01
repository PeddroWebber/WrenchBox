using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Tracking;
using WrenchBox.Application.Features.WorkOrders;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class WorkOrderLifecycleHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static WorkOrder CreateOrderInStatus(WorkOrderStatus targetStatus)
    {
        var customer = Customer.Create("39053344705", "João", "joao@test.com", "11999999999");
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-1", customer.Id, Guid.NewGuid(), [(service, 1)], []);
        typeof(WorkOrder).GetProperty(nameof(WorkOrder.Customer))!.SetValue(order, customer);
        typeof(WorkOrder).GetProperty(nameof(WorkOrder.Vehicle))!
            .SetValue(order, customer.AddVehicle("ABC1D23", "Toyota", "Corolla", 2020));

        if (targetStatus >= WorkOrderStatus.InDiagnosis)
            order.StartDiagnosis();
        if (targetStatus >= WorkOrderStatus.AwaitingApproval)
            order.SendBudgetForApproval();
        if (targetStatus >= WorkOrderStatus.InExecution)
            order.ApproveBudget(new Dictionary<Guid, Part>(), deductStock: false);
        if (targetStatus >= WorkOrderStatus.Completed)
            order.Complete();

        return order;
    }

    [Fact]
    public async Task StartDiagnosis_Success()
    {
        var order = CreateOrderInStatus(WorkOrderStatus.Received);
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new StartDiagnosisCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(new StartDiagnosisCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.InDiagnosis);
    }

    [Fact]
    public async Task StartDiagnosis_NotFound_Throws()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new StartDiagnosisCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(new StartDiagnosisCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Complete_Success()
    {
        var order = CreateOrderInStatus(WorkOrderStatus.InExecution);
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new CompleteWorkOrderCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(new CompleteWorkOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.Completed);
    }

    [Fact]
    public async Task Deliver_Success()
    {
        var order = CreateOrderInStatus(WorkOrderStatus.Completed);
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new DeliverWorkOrderCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(new DeliverWorkOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.Delivered);
    }

    [Fact]
    public async Task GetById_Success()
    {
        var order = CreateOrderInStatus(WorkOrderStatus.Received);
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new GetWorkOrderByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetWorkOrderByIdQuery(order.Id), CancellationToken.None);

        result.OrderNumber.Should().Be("WO-1");
    }

    [Fact]
    public async Task GetById_NotFound_Throws()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new GetWorkOrderByIdQueryHandler(repo.Object);
        var act = async () => await handler.Handle(new GetWorkOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetWorkOrders_ReturnsPagedResult()
    {
        var order = CreateOrderInStatus(WorkOrderStatus.Received);
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetPagedAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<WorkOrder> { order }, 1));

        var handler = new GetWorkOrdersQueryHandler(repo.Object);
        var result = await handler.Handle(new GetWorkOrdersQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }
}

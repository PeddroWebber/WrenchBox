using FluentAssertions;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Tests.Entities;

public class WorkOrderTests
{
    private static Service CreateService(decimal price = 100m) =>
        Service.Create("Oil Change", "Full synthetic oil change", price, 60);

    private static Part CreatePart(int stock = 10, decimal price = 50m) =>
        Part.Create("Oil Filter", "OF-001", price, stock, 2);

  private static WorkOrder CreateWorkOrder(
        IEnumerable<(Service, int)>? services = null,
        IEnumerable<(Part, int)>? parts = null)
    {
        var serviceList = services?.ToList() ?? [(CreateService(), 1)];
        var partList = parts?.ToList() ?? [];
        return WorkOrder.Create("WO-2026-00001", Guid.NewGuid(), Guid.NewGuid(), serviceList, partList);
    }

    [Fact]
    public void Create_CalculatesTotalAmount()
    {
        var order = CreateWorkOrder(
            [(CreateService(100m), 2)],
            [(CreatePart(price: 30m), 1)]);

        order.TotalAmount.Should().Be(230m);
        order.Status.Should().Be(WorkOrderStatus.Received);
    }

    [Fact]
    public void Create_WithoutServices_Throws()
    {
        var act = () => WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(), [], []);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FullLifecycle_TransitionsCorrectly()
    {
        var part = CreatePart();
        var order = CreateWorkOrder(parts: [(part, 2)]);

        order.StartDiagnosis();
        order.Status.Should().Be(WorkOrderStatus.InDiagnosis);

        var token = order.SendBudgetForApproval();
        token.Should().NotBeNullOrEmpty();
        order.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        order.TrackingToken.Should().Be(token);

        var partsById = new Dictionary<Guid, Part> { [part.Id] = part };
        order.ApproveBudget(partsById);
        order.Status.Should().Be(WorkOrderStatus.InExecution);
        part.StockQuantity.Should().Be(8);

        order.Complete();
        order.Status.Should().Be(WorkOrderStatus.Completed);

        order.Deliver();
        order.Status.Should().Be(WorkOrderStatus.Delivered);
    }

    [Fact]
    public void StartDiagnosis_FromWrongStatus_Throws()
    {
        var order = CreateWorkOrder();
        order.StartDiagnosis();
        var act = () => order.StartDiagnosis();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SendBudget_FromReceived_Throws()
    {
        var order = CreateWorkOrder();
        var act = () => order.SendBudgetForApproval();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ApproveBudget_InsufficientStock_Throws()
    {
        var part = CreatePart(stock: 2);
        var order = CreateWorkOrder(parts: [(part, 1)]);
        order.StartDiagnosis();
        order.SendBudgetForApproval();

        part.Deduct(2, null, "External sale");

        var act = () => order.ApproveBudget(new Dictionary<Guid, Part> { [part.Id] = part });
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GetExecutionDuration_ReturnsValue_WhenCompleted()
    {
        var order = CreateWorkOrder();
        order.StartDiagnosis();
        order.SendBudgetForApproval();
        order.ApproveBudget(new Dictionary<Guid, Part>());
        order.Complete();

        order.GetExecutionDuration().Should().NotBeNull();
    }
}

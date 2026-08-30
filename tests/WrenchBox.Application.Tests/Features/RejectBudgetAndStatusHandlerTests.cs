using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Features.Tracking;
using WrenchBox.Application.Features.WorkOrders;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class RejectBudgetAndStatusHandlerTests
{
    private static WorkOrder CreateAwaitingApprovalOrder()
    {
        var customer = Customer.Create("39053344705", "João", "joao@test.com", "11999999999");
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-1", customer.Id, Guid.NewGuid(), [(service, 1)], []);
        typeof(WorkOrder).GetProperty(nameof(WorkOrder.Customer))!.SetValue(order, customer);
        order.StartDiagnosis();
        order.SendBudgetForApproval();
        return order;
    }

    [Fact]
    public async Task RejectBudget_ReturnsToDiagnosis()
    {
        var order = CreateAwaitingApprovalOrder();
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByTrackingTokenForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new RejectBudgetCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object);
        var result = await handler.Handle(new RejectBudgetCommand(order.TrackingToken!), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        result.StatusLabel.Should().Be("Diagnóstico");
    }

    [Fact]
    public async Task RejectBudget_InvalidToken_Throws()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByTrackingTokenForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new RejectBudgetCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object);
        var act = () => handler.Handle(new RejectBudgetCommand("invalid"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetWorkOrderStatus_ReturnsPortugueseLabel()
    {
        var order = CreateAwaitingApprovalOrder();
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new GetWorkOrderStatusQueryHandler(repo.Object);
        var result = await handler.Handle(new GetWorkOrderStatusQuery(order.Id), CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.AwaitingApproval);
        result.StatusLabel.Should().Be("Aguardando Aprovação");
        result.OrderNumber.Should().Be("WO-1");
    }

    [Fact]
    public async Task GetWorkOrderStatus_NotFound_Throws()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new GetWorkOrderStatusQueryHandler(repo.Object);
        var act = () => handler.Handle(new GetWorkOrderStatusQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Webhook_StartDiagnosis_DelegatesToCommand()
    {
        var dto = new DTOs.WorkOrderDto(
            Guid.NewGuid(),
            "WO-1",
            Guid.NewGuid(),
            "João",
            Guid.NewGuid(),
            "ABC1D23",
            WorkOrderStatus.InDiagnosis,
            "Diagnóstico",
            50m,
            null,
            null,
            [],
            [],
            [],
            DateTime.UtcNow);

        var mediator = new Mock<MediatR.IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<StartDiagnosisCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var handler = new UpdateWorkOrderStatusFromWebhookCommandHandler(mediator.Object);
        var result = await handler.Handle(
            new UpdateWorkOrderStatusFromWebhookCommand(dto.Id, "start-diagnosis"),
            CancellationToken.None);

        result.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        result.StatusLabel.Should().Be("Diagnóstico");
    }

    [Fact]
    public async Task Webhook_UnknownAction_Throws()
    {
        var handler = new UpdateWorkOrderStatusFromWebhookCommandHandler(new Mock<MediatR.IMediator>().Object);
        var act = () => handler.Handle(
            new UpdateWorkOrderStatusFromWebhookCommand(Guid.NewGuid(), "explode"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>();
    }
}

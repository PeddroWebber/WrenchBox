using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Auth;
using WrenchBox.Application.Features.Customers;
using WrenchBox.Application.Features.Metrics;
using WrenchBox.Application.Features.WorkOrders;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_InvalidCredentials_ThrowsUnauthorized()
    {
        var userRepo = new Mock<IAdminUserRepository>();
        userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        var handler = new LoginCommandHandler(
            userRepo.Object,
            new Mock<IPasswordHasher>().Object,
            new Mock<IJwtTokenService>().Object);

        var act = () => handler.Handle(new LoginCommand("a@b.com", "pass"), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedApplicationException>();
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var user = AdminUser.Create("admin@test.com", "hash");
        var userRepo = new Mock<IAdminUserRepository>();
        userRepo.Setup(r => r.GetByEmailAsync("admin@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify("pass", "hash")).Returns(true);

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerateToken(user.Id, user.Email, user.Role)).Returns("token-123");

        var handler = new LoginCommandHandler(userRepo.Object, hasher.Object, jwt.Object);
        var result = await handler.Handle(new LoginCommand("admin@test.com", "pass"), CancellationToken.None);

        result.Token.Should().Be("token-123");
    }
}

public class SendBudgetCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTrackingTokenAndSendsNotification()
    {
        var customer = Customer.Create("39053344705", "João", "joao@test.com", "11999999999");
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-1", customer.Id, Guid.NewGuid(), [(service, 1)], []);
        order.StartDiagnosis();

        typeof(WorkOrder).GetProperty(nameof(WorkOrder.Customer))!
            .SetValue(order, customer);

        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var notification = new Mock<IBudgetNotificationService>();
        notification.Setup(n => n.SendBudgetApprovalRequestAsync(
                customer.Email,
                order.OrderNumber,
                order.TotalAmount,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new SendBudgetCommandHandler(
            repo.Object,
            new Mock<IUnitOfWork>().Object,
            notification.Object);

        var result = await handler.Handle(new SendBudgetCommand(order.Id), CancellationToken.None);

        result.TrackingToken.Should().NotBeNullOrEmpty();
        result.NotificationSent.Should().BeTrue();
        order.Status.Should().Be(Domain.Enums.WorkOrderStatus.AwaitingApproval);
        notification.Verify(n => n.SendBudgetApprovalRequestAsync(
            customer.Email,
            order.OrderNumber,
            order.TotalAmount,
            result.TrackingToken,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkOrderNotFound_Throws()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var handler = new SendBudgetCommandHandler(
            repo.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IBudgetNotificationService>().Object);

        var act = async () => await handler.Handle(new SendBudgetCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class GetAverageExecutionTimeQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoOrders_ReturnsZero()
    {
        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetCompletedWithExecutionTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkOrder>());

        var handler = new GetAverageExecutionTimeQueryHandler(repo.Object);
        var result = await handler.Handle(new GetAverageExecutionTimeQuery(), CancellationToken.None);

        result.CompletedOrdersCount.Should().Be(0);
        result.AverageMinutes.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCompletedOrders_ReturnsAverage()
    {
        var service = Service.Create("Oil", "D", 50m, 30);
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(), [(service, 1)], []);
        order.StartDiagnosis();
        order.SendBudgetForApproval();
        order.ApproveBudget(new Dictionary<Guid, Part>(), deductStock: false);
        order.Complete();

        var repo = new Mock<IWorkOrderRepository>();
        repo.Setup(r => r.GetCompletedWithExecutionTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkOrder> { order });

        var handler = new GetAverageExecutionTimeQueryHandler(repo.Object);
        var result = await handler.Handle(new GetAverageExecutionTimeQuery(), CancellationToken.None);

        result.CompletedOrdersCount.Should().Be(1);
        result.AverageMinutes.Should().BeGreaterThanOrEqualTo(0);
    }
}

public class CustomerHandlerTests
{
    [Fact]
    public async Task GetById_NotFound_Throws()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new GetCustomerByIdQueryHandler(repo.Object);
        var act = async () => await handler.Handle(new GetCustomerByIdQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var customer = Customer.Create("39053344705", "João", "joao@test.com", "11999999999");
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var handler = new GetCustomerByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetCustomerByIdQuery(customer.Id), CancellationToken.None);

        result.Name.Should().Be("João");
        result.Document.Should().Be("39053344705");
    }
}

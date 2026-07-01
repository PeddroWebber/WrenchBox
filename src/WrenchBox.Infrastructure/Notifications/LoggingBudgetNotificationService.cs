using Microsoft.Extensions.Logging;
using WrenchBox.Application.Interfaces;

namespace WrenchBox.Infrastructure.Notifications;

public class LoggingBudgetNotificationService : IBudgetNotificationService
{
    private readonly ILogger<LoggingBudgetNotificationService> _logger;

    public LoggingBudgetNotificationService(ILogger<LoggingBudgetNotificationService> logger) =>
        _logger = logger;

    public Task<bool> SendBudgetApprovalRequestAsync(
        string customerEmail,
        string orderNumber,
        decimal totalAmount,
        string trackingToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Budget approval request sent to {Email} for order {OrderNumber}. Total: {Total:C}. Tracking token: {Token}",
            customerEmail,
            orderNumber,
            totalAmount,
            trackingToken);

        return Task.FromResult(true);
    }
}

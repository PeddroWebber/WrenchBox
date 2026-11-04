using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WrenchBox.Application.Interfaces;
using WrenchBox.Application.Notifications;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Infrastructure.Notifications;

public class NotifySettings
{
    public const string SectionName = "Notify";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public static class NotifyClient
{
    public const string Name = "wrenchbox-notify";
}

public class RoutingNotificationService : INotificationService, IBudgetNotificationService
{
    private readonly SmtpNotificationService _smtp;
    private readonly NotifySettings _notify;
    private readonly SmtpSettings _smtpSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOperationalMetrics _metrics;
    private readonly IIntegrationFailureRepository _failures;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoutingNotificationService> _logger;

    public RoutingNotificationService(
        SmtpNotificationService smtp,
        IOptions<NotifySettings> notify,
        IOptions<SmtpSettings> smtpSettings,
        IHttpClientFactory httpClientFactory,
        IOperationalMetrics metrics,
        IIntegrationFailureRepository failures,
        IUnitOfWork unitOfWork,
        ILogger<RoutingNotificationService> logger)
    {
        _smtp = smtp;
        _notify = notify.Value;
        _smtpSettings = smtpSettings.Value;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _failures = failures;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<bool> SendBudgetApprovalRequestAsync(
        string customerEmail,
        string orderNumber,
        decimal totalAmount,
        string trackingToken,
        CancellationToken cancellationToken = default)
    {
        var root = _smtpSettings.PublicBaseUrl.TrimEnd('/');
        var approveUrl = $"{root}/api/v1/tracking/work-orders/decision?approved=true&token={Uri.EscapeDataString(trackingToken)}";
        var rejectUrl = $"{root}/api/v1/tracking/work-orders/decision?approved=false&token={Uri.EscapeDataString(trackingToken)}";

        return DispatchAsync(new WorkOrderNotificationEvent(
            WorkOrderNotificationEvents.BudgetApprovalRequested,
            customerEmail,
            orderNumber,
            totalAmount,
            trackingToken,
            WorkOrderStatus.AwaitingApproval.ToString(),
            WorkOrderStatus.AwaitingApproval.ToPortuguese(),
            approveUrl,
            rejectUrl), cancellationToken);
    }

    public Task<bool> SendStatusChangedAsync(
        string customerEmail,
        string orderNumber,
        WorkOrderStatus status,
        string statusLabel,
        CancellationToken cancellationToken = default)
    {
        return DispatchAsync(new WorkOrderNotificationEvent(
            WorkOrderNotificationEvents.StatusChanged,
            customerEmail,
            orderNumber,
            null,
            null,
            status.ToString(),
            statusLabel,
            null,
            null), cancellationToken);
    }

    private async Task<bool> DispatchAsync(WorkOrderNotificationEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var sent = NotificationRoute.Choose(_notify.BaseUrl) == NotificationChannel.Smtp
                ? await SendSmtpAsync(notification, cancellationToken)
                : await PostFunctionAsync(notification, cancellationToken);

            if (!sent)
                await RecordFailureAsync(notification, null, cancellationToken);

            return sent;
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(notification, ex, cancellationToken);
            return false;
        }
    }

    private Task<bool> SendSmtpAsync(WorkOrderNotificationEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EventType == WorkOrderNotificationEvents.BudgetApprovalRequested)
        {
            return _smtp.SendBudgetApprovalRequestAsync(
                notification.CustomerEmail,
                notification.OrderNumber,
                notification.TotalAmount ?? 0,
                notification.TrackingToken ?? string.Empty,
                cancellationToken);
        }

        return _smtp.SendStatusChangedAsync(
            notification.CustomerEmail,
            notification.OrderNumber,
            Enum.Parse<WorkOrderStatus>(notification.Status!),
            notification.StatusLabel ?? notification.Status!,
            cancellationToken);
    }

    private async Task<bool> PostFunctionAsync(WorkOrderNotificationEvent notification, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(NotifyClient.Name);
        using var response = await client.PostAsJsonAsync("notifications/work-orders", notification, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task RecordFailureAsync(
        WorkOrderNotificationEvent notification,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        _metrics.IntegrationError("notify");
        _logger.LogWarning(
            exception,
            "WorkOrderProcessingFailed OrderNumber={OrderNumber} EventType={EventType}",
            notification.OrderNumber,
            notification.EventType);

        try
        {
            await _failures.AddAsync(
                IntegrationFailure.Create("notify", notification.OrderNumber, notification.EventType),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist integration failure for {OrderNumber}.", notification.OrderNumber);
        }
    }
}

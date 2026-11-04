namespace WrenchBox.Application.Notifications;

public enum NotificationChannel
{
    Smtp,
    Function
}

public static class NotificationRoute
{
    public static NotificationChannel Choose(string? baseUrl) =>
        string.IsNullOrWhiteSpace(baseUrl) ? NotificationChannel.Smtp : NotificationChannel.Function;
}

public static class WorkOrderNotificationEvents
{
    public const string BudgetApprovalRequested = "BudgetApprovalRequested";
    public const string StatusChanged = "WorkOrderStatusChanged";
    public const string ProcessingFailed = "WorkOrderProcessingFailed";
}

public sealed record WorkOrderNotificationEvent(
    string EventType,
    string CustomerEmail,
    string OrderNumber,
    decimal? TotalAmount,
    string? TrackingToken,
    string? Status,
    string? StatusLabel,
    string? ApproveUrl,
    string? RejectUrl);

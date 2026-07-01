namespace WrenchBox.Application.Interfaces;

public interface IBudgetNotificationService
{
    Task<bool> SendBudgetApprovalRequestAsync(
        string customerEmail,
        string orderNumber,
        decimal totalAmount,
        string trackingToken,
        CancellationToken cancellationToken = default);
}

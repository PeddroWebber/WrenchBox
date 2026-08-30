using WrenchBox.Domain.Enums;

namespace WrenchBox.Application.Interfaces;

public interface INotificationService
{
    Task<bool> SendBudgetApprovalRequestAsync(
        string customerEmail,
        string orderNumber,
        decimal totalAmount,
        string trackingToken,
        CancellationToken cancellationToken = default);

    Task<bool> SendStatusChangedAsync(
        string customerEmail,
        string orderNumber,
        WorkOrderStatus status,
        string statusLabel,
        CancellationToken cancellationToken = default);
}

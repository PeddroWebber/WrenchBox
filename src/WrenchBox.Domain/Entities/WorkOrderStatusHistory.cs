using WrenchBox.Domain.Enums;

namespace WrenchBox.Domain.Entities;

public class WorkOrderStatusHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; private set; }
    public WorkOrderStatus FromStatus { get; private set; }
    public WorkOrderStatus ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;
    public string? ChangedBy { get; private set; }

    private WorkOrderStatusHistory() { }

    public static WorkOrderStatusHistory Create(Guid workOrderId, WorkOrderStatus from, WorkOrderStatus to, string? changedBy = null)
    {
        return new WorkOrderStatusHistory
        {
            WorkOrderId = workOrderId,
            FromStatus = from,
            ToStatus = to,
            ChangedBy = changedBy
        };
    }
}

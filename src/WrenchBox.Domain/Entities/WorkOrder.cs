using WrenchBox.Domain.Common;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Entities;

public class WorkOrder : Entity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid VehicleId { get; private set; }
    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.Received;
    public string? TrackingToken { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }

    public DateTime? DiagnosisStartedAt { get; private set; }
    public DateTime? BudgetSentAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ExecutionStartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public Customer? Customer { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    private readonly List<WorkOrderServiceItem> _serviceItems = [];
    private readonly List<WorkOrderPartItem> _partItems = [];
    private readonly List<WorkOrderStatusHistory> _statusHistory = [];

    public IReadOnlyCollection<WorkOrderServiceItem> ServiceItems => _serviceItems.AsReadOnly();
    public IReadOnlyCollection<WorkOrderPartItem> PartItems => _partItems.AsReadOnly();
    public IReadOnlyCollection<WorkOrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private WorkOrder() { }

    public static WorkOrder Create(
        string orderNumber,
        Guid customerId,
        Guid vehicleId,
        IEnumerable<(Service Service, int Quantity)> services,
        IEnumerable<(Part Part, int Quantity)> parts,
        string? notes = null)
    {
        var serviceList = services.ToList();
        if (serviceList.Count == 0)
            throw new DomainException("At least one service is required.");

        var workOrder = new WorkOrder
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            VehicleId = vehicleId,
            Notes = notes?.Trim(),
            Status = WorkOrderStatus.Received
        };

        foreach (var (service, quantity) in serviceList)
        {
            if (!service.IsActive)
                throw new DomainException($"Service '{service.Name}' is not active.");

            workOrder._serviceItems.Add(WorkOrderServiceItem.Create(workOrder.Id, service, quantity));
        }

        foreach (var (part, quantity) in parts)
        {
            if (!part.IsActive)
                throw new DomainException($"Part '{part.Sku}' is not active.");

            workOrder._partItems.Add(WorkOrderPartItem.Create(workOrder.Id, part, quantity));
        }

        workOrder.RecalculateTotal();
        workOrder.RecordStatusChange(WorkOrderStatus.Received, WorkOrderStatus.Received, "Sistema");

        return workOrder;
    }

    public void StartDiagnosis(string? changedBy = null)
    {
        EnsureStatus(WorkOrderStatus.Received, nameof(StartDiagnosis));
        TransitionTo(WorkOrderStatus.InDiagnosis, changedBy);
        DiagnosisStartedAt = DateTime.UtcNow;
    }

    public string SendBudgetForApproval(string? changedBy = null)
    {
        EnsureStatus(WorkOrderStatus.InDiagnosis, nameof(SendBudgetForApproval));

        if (_serviceItems.Count == 0)
            throw new DomainException("Work order must have at least one service.");

        TrackingToken = Guid.NewGuid().ToString("N");
        BudgetSentAt = DateTime.UtcNow;
        TransitionTo(WorkOrderStatus.AwaitingApproval, changedBy);

        return TrackingToken;
    }

    public void ApproveBudget(IReadOnlyDictionary<Guid, Part> partsById, string? changedBy = null, bool deductStock = true)
    {
        EnsureStatus(WorkOrderStatus.AwaitingApproval, nameof(ApproveBudget));

        if (deductStock)
        {
            foreach (var item in _partItems)
            {
                if (!partsById.TryGetValue(item.PartId, out var part))
                    throw new DomainException($"Part '{item.PartSku}' not found.");

                part.Deduct(item.Quantity, Id, $"Work order {OrderNumber} approval");
            }
        }

        ApprovedAt = DateTime.UtcNow;
        ExecutionStartedAt = DateTime.UtcNow;
        TransitionTo(WorkOrderStatus.InExecution, changedBy);
    }

    public void RejectBudget(string? changedBy = null)
    {
        EnsureStatus(WorkOrderStatus.AwaitingApproval, nameof(RejectBudget));
        TrackingToken = null;
        BudgetSentAt = null;
        TransitionTo(WorkOrderStatus.InDiagnosis, changedBy);
    }

    public void Complete(string? changedBy = null)
    {
        EnsureStatus(WorkOrderStatus.InExecution, nameof(Complete));
        CompletedAt = DateTime.UtcNow;
        TransitionTo(WorkOrderStatus.Completed, changedBy);
    }

    public void Deliver(string? changedBy = null)
    {
        EnsureStatus(WorkOrderStatus.Completed, nameof(Deliver));
        DeliveredAt = DateTime.UtcNow;
        TransitionTo(WorkOrderStatus.Delivered, changedBy);
    }

    public bool CanModifyItems() =>
        Status is WorkOrderStatus.Received or WorkOrderStatus.InDiagnosis;

    public void RecalculateTotal()
    {
        TotalAmount = _serviceItems.Sum(i => i.TotalPrice) + _partItems.Sum(i => i.TotalPrice);
        MarkUpdated();
    }

    public TimeSpan? GetExecutionDuration()
    {
        if (ExecutionStartedAt is null || CompletedAt is null)
            return null;

        return CompletedAt.Value - ExecutionStartedAt.Value;
    }

    private void EnsureStatus(WorkOrderStatus expected, string operation)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {operation} when work order status is '{Status}'. Expected '{expected}'.");
    }

    private void TransitionTo(WorkOrderStatus newStatus, string? changedBy)
    {
        var previous = Status;
        Status = newStatus;
        RecordStatusChange(previous, newStatus, changedBy);
        MarkUpdated();
    }

    private void RecordStatusChange(WorkOrderStatus from, WorkOrderStatus to, string? changedBy)
    {
        if (from == to && _statusHistory.Count > 0)
            return;

        _statusHistory.Add(WorkOrderStatusHistory.Create(Id, from, to, changedBy));
    }
}

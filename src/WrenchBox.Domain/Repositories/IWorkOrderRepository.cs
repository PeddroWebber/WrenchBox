using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Domain.Repositories;

public sealed record WorkOrderTiming(
    DateTime CreatedAt,
    DateTime? DiagnosisStartedAt,
    DateTime? BudgetSentAt,
    DateTime? ExecutionStartedAt,
    DateTime? CompletedAt,
    DateTime? DeliveredAt);

public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByTrackingTokenForUpdateAsync(string trackingToken, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WorkOrder> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        WorkOrderStatus? status,
        Guid? customerId,
        bool includeClosed = false,
        CancellationToken cancellationToken = default);
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrder>> GetCompletedWithExecutionTimesAsync(CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByIdWithPartsForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderTiming>> ListTimingsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
    void Update(WorkOrder workOrder);
}

using MediatR;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Metrics;

public record GetAverageExecutionTimeQuery : IRequest<AverageExecutionTimeDto>;
public record GetOperationalMetricsQuery(DateTime? DayUtc = null) : IRequest<OperationalMetricsDto>;

public class GetAverageExecutionTimeQueryHandler : IRequestHandler<GetAverageExecutionTimeQuery, AverageExecutionTimeDto>
{
    private readonly IWorkOrderRepository _repository;

    public GetAverageExecutionTimeQueryHandler(IWorkOrderRepository repository) => _repository = repository;

    public async Task<AverageExecutionTimeDto> Handle(GetAverageExecutionTimeQuery request, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetCompletedWithExecutionTimesAsync(cancellationToken);
        var durations = orders
            .Select(o => o.GetExecutionDuration())
            .Where(d => d.HasValue)
            .Select(d => d!.Value.TotalMinutes)
            .ToList();

        if (durations.Count == 0)
            return new AverageExecutionTimeDto(0, 0);

        return new AverageExecutionTimeDto(durations.Average(), durations.Count);
    }
}

public class GetOperationalMetricsQueryHandler : IRequestHandler<GetOperationalMetricsQuery, OperationalMetricsDto>
{
    private readonly IWorkOrderRepository _workOrders;
    private readonly IIntegrationFailureRepository _failures;

    public GetOperationalMetricsQueryHandler(IWorkOrderRepository workOrders, IIntegrationFailureRepository failures)
    {
        _workOrders = workOrders;
        _failures = failures;
    }

    public async Task<OperationalMetricsDto> Handle(GetOperationalMetricsQuery request, CancellationToken cancellationToken)
    {
        var day = DateTime.SpecifyKind((request.DayUtc ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var nextDay = day.AddDays(1);
        var timings = await _workOrders.ListTimingsAsync(cancellationToken);

        var openedToday = timings.Count(t => t.CreatedAt >= day && t.CreatedAt < nextDay);
        var diagnosis = timings
            .Where(t => t.DiagnosisStartedAt is not null && t.BudgetSentAt is not null)
            .Select(t => (t.BudgetSentAt!.Value - t.DiagnosisStartedAt!.Value).TotalMinutes)
            .ToList();
        var execution = timings
            .Where(t => t.ExecutionStartedAt is not null && t.CompletedAt is not null)
            .Select(t => (t.CompletedAt!.Value - t.ExecutionStartedAt!.Value).TotalMinutes)
            .ToList();
        var finalization = timings
            .Where(t => t.CompletedAt is not null && t.DeliveredAt is not null)
            .Select(t => (t.DeliveredAt!.Value - t.CompletedAt!.Value).TotalMinutes)
            .ToList();
        var failures = await _failures.CountSinceAsync(day, cancellationToken);

        return new OperationalMetricsDto(
            day,
            openedToday,
            diagnosis.Count == 0 ? 0 : diagnosis.Average(),
            diagnosis.Count,
            execution.Count == 0 ? 0 : execution.Average(),
            execution.Count,
            finalization.Count == 0 ? 0 : finalization.Average(),
            finalization.Count,
            failures);
    }
}

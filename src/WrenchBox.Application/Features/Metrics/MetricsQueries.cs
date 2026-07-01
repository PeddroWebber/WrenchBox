using MediatR;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Metrics;

public record GetAverageExecutionTimeQuery : IRequest<AverageExecutionTimeDto>;

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

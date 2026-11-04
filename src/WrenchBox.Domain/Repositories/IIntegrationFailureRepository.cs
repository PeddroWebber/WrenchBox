using WrenchBox.Domain.Entities;

namespace WrenchBox.Domain.Repositories;

public interface IIntegrationFailureRepository
{
    Task AddAsync(IntegrationFailure failure, CancellationToken cancellationToken = default);
    Task<int> CountSinceAsync(DateTime utcFrom, CancellationToken cancellationToken = default);
}

using WrenchBox.Domain.Entities;

namespace WrenchBox.Domain.Repositories;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? activeOnly, CancellationToken cancellationToken = default);
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    void Update(Service service);
    void Remove(Service service);
}

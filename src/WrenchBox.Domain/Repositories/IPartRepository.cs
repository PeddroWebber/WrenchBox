using WrenchBox.Domain.Entities;

namespace WrenchBox.Domain.Repositories;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Part?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Part>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Part>> GetByIdsForUpdateAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Part> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? activeOnly, CancellationToken cancellationToken = default);
    Task AddAsync(Part part, CancellationToken cancellationToken = default);
    void Update(Part part);
    void Remove(Part part);
}

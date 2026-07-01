using WrenchBox.Domain.Entities;

namespace WrenchBox.Domain.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> GetPagedAsync(Guid? customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    void Update(Vehicle vehicle);
    void Remove(Vehicle vehicle);
}

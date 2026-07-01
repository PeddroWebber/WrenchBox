using WrenchBox.Domain.Entities;

namespace WrenchBox.Domain.Repositories;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(AdminUser user, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Repositories;
using WrenchBox.Infrastructure.Persistence;

namespace WrenchBox.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly WrenchBoxDbContext _context;

    public CustomerRepository(WrenchBoxDbContext context) => _context = context;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default) =>
        await _context.Customers.FirstOrDefaultAsync(c => c.Document == document, cancellationToken);

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.Document.Contains(term) || c.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await _context.Customers.AddAsync(customer, cancellationToken);

    public void Update(Customer customer) => _context.Customers.Update(customer);

    public void Remove(Customer customer) => _context.Customers.Remove(customer);
}

public class VehicleRepository : IVehicleRepository
{
    private readonly WrenchBoxDbContext _context;

    public VehicleRepository(WrenchBoxDbContext context) => _context = context;

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        var normalized = plate.Replace("-", "").ToUpperInvariant();
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Plate == normalized, cancellationToken);
    }

    public async Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> GetPagedAsync(Guid? customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Vehicles.AsQueryable();
        if (customerId.HasValue)
            query = query.Where(v => v.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);

    public void Update(Vehicle vehicle) => _context.Vehicles.Update(vehicle);

    public void Remove(Vehicle vehicle) => _context.Vehicles.Remove(vehicle);
}

public class ServiceRepository : IServiceRepository
{
    private readonly WrenchBoxDbContext _context;

    public ServiceRepository(WrenchBoxDbContext context) => _context = context;

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Service>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await _context.Services.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.AsQueryable();
        if (activeOnly == true)
            query = query.Where(s => s.IsActive);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default) =>
        await _context.Services.AddAsync(service, cancellationToken);

    public void Update(Service service) => _context.Services.Update(service);

    public void Remove(Service service) => _context.Services.Remove(service);
}

public class PartRepository : IPartRepository
{
    private readonly WrenchBoxDbContext _context;

    public PartRepository(WrenchBoxDbContext context) => _context = context;

    public async Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Parts.Include(p => p.StockMovements).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Part?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Parts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Part>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await _context.Parts.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Part>> GetByIdsForUpdateAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await _context.Parts.Include(p => p.StockMovements).Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Part> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.Parts.AsQueryable();
        if (activeOnly == true)
            query = query.Where(p => p.IsActive);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Part part, CancellationToken cancellationToken = default) =>
        await _context.Parts.AddAsync(part, cancellationToken);

    public void Update(Part part)
    {
        if (_context.Entry(part).State == EntityState.Detached)
            _context.Parts.Update(part);
    }

    public void Remove(Part part) => _context.Parts.Remove(part);
}

public class WorkOrderRepository : IWorkOrderRepository
{
    private readonly WrenchBoxDbContext _context;

    public WorkOrderRepository(WrenchBoxDbContext context) => _context = context;

    private IQueryable<WorkOrder> WithIncludes() =>
        _context.WorkOrders
            .Include(w => w.Customer)
            .Include(w => w.Vehicle)
            .Include(w => w.ServiceItems)
            .Include(w => w.PartItems)
            .Include(w => w.StatusHistory);

    public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await WithIncludes().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<WorkOrder?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.WorkOrders
            .Include(w => w.ServiceItems)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<WorkOrder?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken = default) =>
        await WithIncludes().FirstOrDefaultAsync(w => w.TrackingToken == trackingToken, cancellationToken);

    public async Task<WorkOrder?> GetByTrackingTokenForUpdateAsync(string trackingToken, CancellationToken cancellationToken = default) =>
        await _context.WorkOrders
            .Include(w => w.PartItems)
            .FirstOrDefaultAsync(w => w.TrackingToken == trackingToken, cancellationToken);

    public async Task<(IReadOnlyList<WorkOrder> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        WorkOrderStatus? status,
        Guid? customerId,
        bool includeClosed = false,
        CancellationToken cancellationToken = default)
    {
        var query = WithIncludes();

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);
        else if (!includeClosed)
            query = query.Where(w => w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Delivered);

        if (customerId.HasValue)
            query = query.Where(w => w.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(w =>
                w.Status == WorkOrderStatus.InExecution ? 0 :
                w.Status == WorkOrderStatus.AwaitingApproval ? 1 :
                w.Status == WorkOrderStatus.InDiagnosis ? 2 :
                w.Status == WorkOrderStatus.Received ? 3 : 4)
            .ThenBy(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"WO-{year}-";

        var sequences = await _context.WorkOrders
            .Where(w => w.OrderNumber.StartsWith(prefix))
            .Select(w => w.OrderNumber)
            .ToListAsync(cancellationToken);

        var maxSequence = sequences
            .Select(number =>
            {
                var parts = number.Split('-');
                return parts.Length == 3 && int.TryParse(parts[2], out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{maxSequence + 1:D5}";
    }

    public async Task<IReadOnlyList<WorkOrder>> GetCompletedWithExecutionTimesAsync(CancellationToken cancellationToken = default) =>
        await _context.WorkOrders
            .Where(w => w.ExecutionStartedAt != null && w.CompletedAt != null)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default) =>
        await _context.WorkOrders.AddAsync(workOrder, cancellationToken);

    public void Update(WorkOrder workOrder)
    {
        if (_context.Entry(workOrder).State == EntityState.Detached)
            _context.WorkOrders.Update(workOrder);
    }
}

public class AdminUserRepository : IAdminUserRepository
{
    private readonly WrenchBoxDbContext _context;

    public AdminUserRepository(WrenchBoxDbContext context) => _context = context;

    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task AddAsync(AdminUser user, CancellationToken cancellationToken = default) =>
        await _context.AdminUsers.AddAsync(user, cancellationToken);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly WrenchBoxDbContext _context;

    public UnitOfWork(WrenchBoxDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using WrenchBox.Domain.Entities;

namespace WrenchBox.Infrastructure.Persistence;

public class WrenchBoxDbContext : DbContext
{
    public WrenchBoxDbContext(DbContextOptions<WrenchBoxDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderServiceItem> WorkOrderServiceItems => Set<WorkOrderServiceItem>();
    public DbSet<WorkOrderPartItem> WorkOrderPartItems => Set<WorkOrderPartItem>();
    public DbSet<WorkOrderStatusHistory> WorkOrderStatusHistories => Set<WorkOrderStatusHistory>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WrenchBoxDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await FixClientGeneratedChildEntityStatesAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        FixClientGeneratedChildEntityStatesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    private async Task FixClientGeneratedChildEntityStatesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in ChangeTracker.Entries<WorkOrderStatusHistory>()
            .Where(e => e.State == EntityState.Modified))
        {
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            if (databaseValues is null)
                entry.State = EntityState.Added;
        }

        foreach (var entry in ChangeTracker.Entries<StockMovement>()
            .Where(e => e.State == EntityState.Modified))
        {
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            if (databaseValues is null)
                entry.State = EntityState.Added;
        }
    }
}

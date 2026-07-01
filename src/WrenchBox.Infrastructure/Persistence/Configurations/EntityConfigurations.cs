using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WrenchBox.Domain.Entities;

namespace WrenchBox.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Document).HasMaxLength(14).IsRequired();
        builder.HasIndex(c => c.Document).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.HasMany(c => c.Vehicles).WithOne(v => v.Customer).HasForeignKey(v => v.CustomerId);
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Plate).HasMaxLength(7).IsRequired();
        builder.HasIndex(v => v.Plate).IsUnique();
        builder.Property(v => v.Brand).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100).IsRequired();
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.UnitPrice).HasPrecision(18, 2);
    }
}

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.UnitPrice).HasPrecision(18, 2);
        builder.HasMany(p => p.StockMovements).WithOne().HasForeignKey(sm => sm.PartId);
        builder.Navigation(p => p.StockMovements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.Reason).HasMaxLength(500).IsRequired();
    }
}

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.OrderNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(w => w.OrderNumber).IsUnique();
        builder.Property(w => w.TrackingToken).HasMaxLength(64);
        builder.HasIndex(w => w.TrackingToken).IsUnique();
        builder.Property(w => w.TotalAmount).HasPrecision(18, 2);
        builder.Property(w => w.Notes).HasMaxLength(1000);
        builder.HasOne(w => w.Customer).WithMany().HasForeignKey(w => w.CustomerId);
        builder.HasOne(w => w.Vehicle).WithMany().HasForeignKey(w => w.VehicleId);
        builder.HasMany(w => w.ServiceItems).WithOne().HasForeignKey(i => i.WorkOrderId);
        builder.HasMany(w => w.PartItems).WithOne().HasForeignKey(i => i.WorkOrderId);
        builder.HasMany(w => w.StatusHistory).WithOne().HasForeignKey(h => h.WorkOrderId);
        builder.Navigation(w => w.ServiceItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(w => w.PartItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(w => w.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class WorkOrderServiceItemConfiguration : IEntityTypeConfiguration<WorkOrderServiceItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderServiceItem> builder)
    {
        builder.ToTable("work_order_service_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(i => i.TotalPrice);
    }
}

public class WorkOrderPartItemConfiguration : IEntityTypeConfiguration<WorkOrderPartItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderPartItem> builder)
    {
        builder.ToTable("work_order_part_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.PartName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.PartSku).HasMaxLength(50).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(i => i.TotalPrice);
    }
}

public class WorkOrderStatusHistoryConfiguration : IEntityTypeConfiguration<WorkOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<WorkOrderStatusHistory> builder)
    {
        builder.ToTable("work_order_status_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ChangedBy).HasMaxLength(100);
    }
}

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(50).IsRequired();
    }
}

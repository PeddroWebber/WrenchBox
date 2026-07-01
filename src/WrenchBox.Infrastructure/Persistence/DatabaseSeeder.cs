using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Entities;

namespace WrenchBox.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WrenchBoxDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WrenchBoxDbContext>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var useLightSeed = configuration.GetValue<bool>("Seed:UseLightSeed");

        await context.Database.MigrateAsync();

        if (!await context.AdminUsers.AnyAsync())
        {
            var admin = AdminUser.Create("admin@wrenchbox.local", passwordHasher.Hash("Admin@123"));
            await context.AdminUsers.AddAsync(admin);
            await context.SaveChangesAsync();
            logger.LogInformation("Administrador padrão criado: admin@wrenchbox.local");
        }

        await BulkDataSeeder.SeedAsync(context, logger, useLightSeed);
    }

    public static async Task ResetAndReseedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WrenchBoxDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WrenchBoxDbContext>>();
        var useLightSeed = configuration.GetValue<bool>("Seed:UseLightSeed");

        await context.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE work_order_status_history, work_order_part_items,
            work_order_service_items, work_orders, stock_movements, parts,
            services, vehicles, customers RESTART IDENTITY CASCADE;
            """);

        context.ChangeTracker.Clear();
        await BulkDataSeeder.SeedAsync(context, logger, useLightSeed);
    }
}

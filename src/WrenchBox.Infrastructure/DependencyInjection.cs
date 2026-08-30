using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Repositories;
using WrenchBox.Infrastructure.Auth;
using WrenchBox.Infrastructure.Persistence;
using WrenchBox.Infrastructure.Notifications;
using WrenchBox.Infrastructure.Repositories;

namespace WrenchBox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<WebhookSettings>(configuration.GetSection(WebhookSettings.SectionName));

        services.AddDbContext<WrenchBoxDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<SmtpNotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<SmtpNotificationService>());
        services.AddScoped<IBudgetNotificationService>(sp => sp.GetRequiredService<SmtpNotificationService>());

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Repositories;
using WrenchBox.Infrastructure.Auth;
using WrenchBox.Infrastructure.Observability;
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
        services.Configure<NotifySettings>(configuration.GetSection(NotifySettings.SectionName));
        services.Configure<WebhookSettings>(configuration.GetSection(WebhookSettings.SectionName));

        services.AddDbContext<WrenchBoxDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IIntegrationFailureRepository, IntegrationFailureRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IOperationalMetrics, OperationalMetrics>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<SmtpNotificationService>();
        services.AddHttpClient(NotifyClient.Name, (sp, client) =>
        {
            var notify = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NotifySettings>>().Value;
            if (!string.IsNullOrWhiteSpace(notify.BaseUrl))
                client.BaseAddress = new Uri(notify.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(notify.ApiKey))
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-functions-key", notify.ApiKey);
        });
        services.AddScoped<RoutingNotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<RoutingNotificationService>());
        services.AddScoped<IBudgetNotificationService>(sp => sp.GetRequiredService<RoutingNotificationService>());

        return services;
    }
}

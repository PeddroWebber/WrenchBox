using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using WrenchBox.Infrastructure.Persistence;

namespace WrenchBox.Integration.Tests;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<WrenchBoxApiFactory>;

public class WrenchBoxApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    public bool IsReady { get; private set; }
    public string SkipReason { get; private set; } = "Docker is not available.";

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("wrenchbox_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();
            IsReady = true;
        }
        catch (Exception ex)
        {
            IsReady = false;
            SkipReason = $"Docker is not available: {ex.Message}";
        }
    }

    public new async Task DisposeAsync()
    {
        if (_postgres is not null)
            await _postgres.DisposeAsync();

        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (!IsReady)
            return;

        await DatabaseSeeder.ResetAndReseedAsync(Services);
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        if (_postgres is null)
            return;

        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Seed:UseLightSeed", "true");
        builder.UseSetting("Jwt:Secret", "WrenchBox_Test_Secret_Key_Min_32_Chars!");
        builder.UseSetting("Jwt:Issuer", "WrenchBox");
        builder.UseSetting("Jwt:Audience", "WrenchBox.Admin");
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Integration.Tests;

[Collection("Integration")]
public class AllEndpointsTests : IAsyncLifetime
{
    private readonly WrenchBoxApiFactory _factory;

    public AllEndpointsTests(WrenchBoxApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task Auth_Login_WithValidCredentials_ReturnsToken()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@wrenchbox.local",
            password = "Admin@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(IntegrationTestHelpers.JsonOptions);
        login!.Token.Should().NotBeNullOrWhiteSpace();
        login.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [SkippableFact]
    public async Task Auth_Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@wrenchbox.local",
            password = "wrong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task AdminEndpoints_WithoutToken_ReturnUnauthorized()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = _factory.CreateClient();

        (await client.GetAsync("/api/v1/customers")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/vehicles")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/services?pageSize=500")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/parts?pageSize=500")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/work-orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/metrics/average-execution-time")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Customers_Endpoints_Succeed()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var list = await client.GetFromJsonAsync<PagedResult<CustomerDto>>("/api/v1/customers", IntegrationTestHelpers.JsonOptions);
        list!.Items.Should().NotBeEmpty();
        list.Items.Should().Contain(c => c.Name == "João Silva");

        var seeded = list.Items.First(c => c.Document == "39053344705");
        var byId = await client.GetFromJsonAsync<CustomerDto>($"/api/v1/customers/{seeded.Id}", IntegrationTestHelpers.JsonOptions);
        byId!.Name.Should().Be("João Silva");

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            document = "52998224725",
            name = "Endpoint Test Customer",
            email = "endpoint@test.com",
            phone = "11955554444"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>(IntegrationTestHelpers.JsonOptions);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/customers/{created!.Id}", new
        {
            name = "Endpoint Test Customer Updated",
            email = "endpoint-updated@test.com",
            phone = "11944443333"
        });
        updateResponse.EnsureSuccessStatusCode();

        (await client.DeleteAsync($"/api/v1/customers/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [SkippableFact]
    public async Task Vehicles_Endpoints_Succeed()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var customers = await client.GetFromJsonAsync<PagedResult<CustomerDto>>("/api/v1/customers", IntegrationTestHelpers.JsonOptions);
        var customer = customers!.Items.First(c => c.Document == "39053344705");

        var list = await client.GetFromJsonAsync<PagedResult<VehicleDto>>("/api/v1/vehicles", IntegrationTestHelpers.JsonOptions);
        list!.Items.Should().NotBeEmpty();

        var filtered = await client.GetFromJsonAsync<PagedResult<VehicleDto>>(
            $"/api/v1/vehicles?customerId={customer.Id}", IntegrationTestHelpers.JsonOptions);
        filtered!.Items.Should().NotBeEmpty();
        filtered.Items.Should().OnlyContain(v => v.CustomerId == customer.Id);

        var seeded = list.Items.First(v => v.Plate == "ABC1D23");
        var byId = await client.GetFromJsonAsync<VehicleDto>($"/api/v1/vehicles/{seeded.Id}", IntegrationTestHelpers.JsonOptions);
        byId!.Brand.Should().Be("Toyota");

        var createResponse = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = "JKL4M56",
            brand = "Volkswagen",
            model = "Gol",
            year = 2018
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>(IntegrationTestHelpers.JsonOptions);

        (await client.PutAsJsonAsync($"/api/v1/vehicles/{created!.Id}", new
        {
            brand = "Volkswagen",
            model = "Gol G7",
            year = 2019
        })).EnsureSuccessStatusCode();

        (await client.DeleteAsync($"/api/v1/vehicles/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [SkippableFact]
    public async Task Services_Endpoints_Succeed()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var list = await client.GetFromJsonAsync<PagedResult<ServiceDto>>("/api/v1/services?pageSize=500", IntegrationTestHelpers.JsonOptions);
        list!.Items.Should().HaveCountGreaterThanOrEqualTo(3);

        var seeded = list.Items.First(s => s.Name == "Troca de Óleo");
        var byId = await client.GetFromJsonAsync<ServiceDto>($"/api/v1/services/{seeded.Id}", IntegrationTestHelpers.JsonOptions);
        byId!.UnitPrice.Should().Be(150m);

        var createResponse = await client.PostAsJsonAsync("/api/v1/services", new
        {
            name = "Test Service",
            description = "Integration test service",
            unitPrice = 99.90m,
            estimatedDurationMinutes = 30
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceDto>(IntegrationTestHelpers.JsonOptions);

        (await client.PutAsJsonAsync($"/api/v1/services/{created!.Id}", new
        {
            name = "Test Service Updated",
            description = "Updated description",
            unitPrice = 109.90m,
            estimatedDurationMinutes = 45,
            isActive = true
        })).EnsureSuccessStatusCode();

        (await client.DeleteAsync($"/api/v1/services/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [SkippableFact]
    public async Task Parts_Endpoints_Succeed()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var list = await client.GetFromJsonAsync<PagedResult<PartDto>>("/api/v1/parts?pageSize=500", IntegrationTestHelpers.JsonOptions);
        list!.Items.Should().HaveCountGreaterThanOrEqualTo(3);

        var seeded = list.Items.First(p => p.Sku == "OIL-5W30");
        var byId = await client.GetFromJsonAsync<PartDto>($"/api/v1/parts/{seeded.Id}", IntegrationTestHelpers.JsonOptions);
        byId!.StockQuantity.Should().BeGreaterThan(50);

        var createResponse = await client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = "Test Part",
            sku = "TST-PART-001",
            unitPrice = 15m,
            stockQuantity = 20,
            minimumStock = 5
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<PartDto>(IntegrationTestHelpers.JsonOptions);

        var stockResponse = await client.PatchAsJsonAsync($"/api/v1/parts/{created!.Id}/stock", new
        {
            quantity = 5,
            reason = "Integration test adjustment"
        });
        stockResponse.EnsureSuccessStatusCode();
        var adjusted = await stockResponse.Content.ReadFromJsonAsync<PartDto>(IntegrationTestHelpers.JsonOptions);
        adjusted!.StockQuantity.Should().Be(25);

        (await client.PutAsJsonAsync($"/api/v1/parts/{created.Id}", new
        {
            name = "Test Part Updated",
            unitPrice = 18m,
            minimumStock = 8,
            isActive = true
        })).EnsureSuccessStatusCode();

        (await client.DeleteAsync($"/api/v1/parts/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [SkippableFact]
    public async Task WorkOrders_Endpoints_Succeed()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var list = await client.GetFromJsonAsync<PagedResult<WorkOrderDto>>("/api/v1/work-orders?pageSize=500", IntegrationTestHelpers.JsonOptions);
        list!.Items.Should().HaveCountGreaterThanOrEqualTo(1);

        var received = list.Items.First(w => w.OrderNumber == "WO-2026-00001");
        var byId = await client.GetFromJsonAsync<WorkOrderDto>($"/api/v1/work-orders/{received.Id}", IntegrationTestHelpers.JsonOptions);
        byId!.OrderNumber.Should().Be("WO-2026-00001");

        var filtered = await client.GetFromJsonAsync<PagedResult<WorkOrderDto>>(
            $"/api/v1/work-orders?status={WorkOrderStatus.Received}&pageSize=500", IntegrationTestHelpers.JsonOptions);
        filtered!.Items.Should().Contain(w => w.OrderNumber == "WO-2026-00001");

        var services = await client.GetFromJsonAsync<PagedResult<ServiceDto>>("/api/v1/services?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var parts = await client.GetFromJsonAsync<PagedResult<PartDto>>("/api/v1/parts?pageSize=500", IntegrationTestHelpers.JsonOptions);

        var oilService = services!.Items.First(s => s.Name == "Troca de Óleo");
        var oilPart = parts!.Items.First(p => p.Sku == "OIL-5W30");

        var createResponse = await client.PostAsJsonAsync("/api/v1/work-orders", new
        {
            customerDocument = "86288308048",
            customerName = "Work Order Test Customer",
            customerEmail = "wo-test@example.com",
            customerPhone = "11933332222",
            vehiclePlate = "MNO5P67",
            vehicleBrand = "Ford",
            vehicleModel = "Ka",
            vehicleYear = 2022,
            services = new[] { new { serviceId = oilService.Id, quantity = 1 } },
            parts = new[] { new { partId = oilPart.Id, quantity = 1 } },
            notes = "Endpoint test work order"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkOrderDto>(IntegrationTestHelpers.JsonOptions);

        (await client.PostAsync($"/api/v1/work-orders/{created!.Id}/start-diagnosis", null)).EnsureSuccessStatusCode();

        var budgetResponse = await client.PostAsync($"/api/v1/work-orders/{created.Id}/send-budget", null);
        budgetResponse.EnsureSuccessStatusCode();
        var budget = await budgetResponse.Content.ReadFromJsonAsync<SendBudgetResponseDto>(IntegrationTestHelpers.JsonOptions);

        var trackingClient = _factory.CreateClient();
        trackingClient.DefaultRequestHeaders.Add("X-Tracking-Token", budget!.TrackingToken);
        (await trackingClient.PostAsync("/api/v1/tracking/work-orders/approve", null)).EnsureSuccessStatusCode();

        (await client.PostAsync($"/api/v1/work-orders/{created.Id}/complete", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/v1/work-orders/{created.Id}/deliver", null)).EnsureSuccessStatusCode();

        var finalOrder = await client.GetFromJsonAsync<WorkOrderDto>($"/api/v1/work-orders/{created.Id}", IntegrationTestHelpers.JsonOptions);
        finalOrder!.Status.Should().Be(WorkOrderStatus.Delivered);
    }

    [SkippableFact]
    public async Task Tracking_Endpoints_Succeed()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var services = await client.GetFromJsonAsync<PagedResult<ServiceDto>>("/api/v1/services?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var parts = await client.GetFromJsonAsync<PagedResult<PartDto>>("/api/v1/parts?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var oilService = services!.Items.First(s => s.Name == "Troca de Óleo");
        var oilPart = parts!.Items.First(p => p.Sku == "FLT-OIL-001");

        var createResponse = await client.PostAsJsonAsync("/api/v1/work-orders", new
        {
            customerDocument = "71428793860",
            customerName = "Tracking Test Customer",
            customerEmail = "tracking@test.com",
            customerPhone = "11911110000",
            vehiclePlate = "RST7U89",
            vehicleBrand = "Hyundai",
            vehicleModel = "HB20",
            vehicleYear = 2021,
            services = new[] { new { serviceId = oilService.Id, quantity = 1 } },
            parts = new[] { new { partId = oilPart.Id, quantity = 1 } },
            notes = "Tracking endpoint test"
        });
        createResponse.EnsureSuccessStatusCode();
        var workOrder = await createResponse.Content.ReadFromJsonAsync<WorkOrderDto>(IntegrationTestHelpers.JsonOptions);

        await client.PostAsync($"/api/v1/work-orders/{workOrder!.Id}/start-diagnosis", null);
        var budgetResponse = await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/send-budget", null);
        budgetResponse.EnsureSuccessStatusCode();
        var budget = await budgetResponse.Content.ReadFromJsonAsync<SendBudgetResponseDto>(IntegrationTestHelpers.JsonOptions);

        var trackingClient = _factory.CreateClient();
        trackingClient.DefaultRequestHeaders.Add("X-Tracking-Token", budget!.TrackingToken);

        var getResponse = await trackingClient.GetAsync("/api/v1/tracking/work-orders");
        getResponse.EnsureSuccessStatusCode();
        var tracking = await getResponse.Content.ReadFromJsonAsync<TrackingWorkOrderDto>(IntegrationTestHelpers.JsonOptions);
        tracking!.Status.Should().Be(WorkOrderStatus.AwaitingApproval);

        var approveResponse = await trackingClient.PostAsync("/api/v1/tracking/work-orders/approve", null);
        approveResponse.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task Tracking_WithoutToken_ReturnsBadRequest()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = _factory.CreateClient();

        (await client.GetAsync("/api/v1/tracking/work-orders")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsync("/api/v1/tracking/work-orders/approve", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Metrics_AverageExecutionTime_ReturnsDataAfterCompletedOrder()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var services = await client.GetFromJsonAsync<PagedResult<ServiceDto>>("/api/v1/services?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var oilService = services!.Items.First(s => s.Name == "Troca de Óleo");

        var createResponse = await client.PostAsJsonAsync("/api/v1/work-orders", new
        {
            customerDocument = "60783311087",
            customerName = "Metrics Test Customer",
            customerEmail = "metrics@test.com",
            customerPhone = "11922221111",
            vehiclePlate = "PQR6S78",
            vehicleBrand = "Chevrolet",
            vehicleModel = "Onix",
            vehicleYear = 2023,
            services = new[] { new { serviceId = oilService.Id, quantity = 1 } },
            parts = Array.Empty<object>(),
            notes = "Metrics test order"
        });
        createResponse.EnsureSuccessStatusCode();
        var workOrder = await createResponse.Content.ReadFromJsonAsync<WorkOrderDto>(IntegrationTestHelpers.JsonOptions);

        (await client.PostAsync($"/api/v1/work-orders/{workOrder!.Id}/start-diagnosis", null)).EnsureSuccessStatusCode();
        var budgetResponse = await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/send-budget", null);
        budgetResponse.EnsureSuccessStatusCode();
        var budget = await budgetResponse.Content.ReadFromJsonAsync<SendBudgetResponseDto>(IntegrationTestHelpers.JsonOptions);

        var trackingClient = _factory.CreateClient();
        trackingClient.DefaultRequestHeaders.Add("X-Tracking-Token", budget!.TrackingToken);
        await trackingClient.PostAsync("/api/v1/tracking/work-orders/approve", null);

        await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/complete", null);

        var response = await client.GetAsync("/api/v1/metrics/average-execution-time");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<AverageExecutionTimeDto>(IntegrationTestHelpers.JsonOptions);
        metrics!.CompletedOrdersCount.Should().BeGreaterThanOrEqualTo(1);
        metrics.AverageMinutes.Should().BeGreaterThanOrEqualTo(0);
    }
}

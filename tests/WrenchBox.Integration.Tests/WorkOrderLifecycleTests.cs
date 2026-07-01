using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Integration.Tests;

[Collection("Integration")]
public class WorkOrderLifecycleTests : IAsyncLifetime
{
    private readonly WrenchBoxApiFactory _factory;

    public WorkOrderLifecycleTests(WrenchBoxApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task FullWorkOrderLifecycle_Succeeds()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);

        var servicesPage = await client.GetFromJsonAsync<PagedResult<ServiceDto>>("/api/v1/services?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var service = servicesPage!.Items.First(s => s.Name == "Troca de Óleo");

        var partsPage = await client.GetFromJsonAsync<PagedResult<PartDto>>("/api/v1/parts?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var part = partsPage!.Items.First(p => p.Sku == "OIL-5W30");

        var createRequest = new
        {
            customerDocument = "39053344705",
            customerName = "Integration Test Customer",
            customerEmail = "integration@test.com",
            customerPhone = "11988887777",
            vehiclePlate = "XYZ9A87",
            vehicleBrand = "Honda",
            vehicleModel = "Civic",
            vehicleYear = 2021,
            services = new[] { new { serviceId = service.Id, quantity = 1 } },
            parts = new[] { new { partId = part.Id, quantity = 1 } },
            notes = "Integration test order"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/work-orders", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var workOrder = await createResponse.Content.ReadFromJsonAsync<WorkOrderDto>(IntegrationTestHelpers.JsonOptions);

        workOrder.Should().NotBeNull();
        workOrder!.Status.Should().Be(WorkOrderStatus.Received);

        (await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/start-diagnosis", null)).EnsureSuccessStatusCode();

        var budgetResponse = await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/send-budget", null);
        budgetResponse.EnsureSuccessStatusCode();
        var budget = await budgetResponse.Content.ReadFromJsonAsync<SendBudgetResponseDto>(IntegrationTestHelpers.JsonOptions);

        var trackingClient = _factory.CreateClient();
        trackingClient.DefaultRequestHeaders.Add("X-Tracking-Token", budget!.TrackingToken);

        (await trackingClient.GetAsync("/api/v1/tracking/work-orders")).EnsureSuccessStatusCode();
        (await trackingClient.PostAsync("/api/v1/tracking/work-orders/approve", null)).EnsureSuccessStatusCode();

        (await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/complete", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/v1/work-orders/{workOrder.Id}/deliver", null)).EnsureSuccessStatusCode();

        var finalOrder = await client.GetFromJsonAsync<WorkOrderDto>($"/api/v1/work-orders/{workOrder.Id}", IntegrationTestHelpers.JsonOptions);
        finalOrder!.Status.Should().Be(WorkOrderStatus.Delivered);
    }
}

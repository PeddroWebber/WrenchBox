using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WrenchBox.Application.DTOs;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Integration.Tests;

[Collection("Integration")]
public class Phase2WorkOrderTests : IAsyncLifetime
{
    private readonly WrenchBoxApiFactory _factory;

    public Phase2WorkOrderTests(WrenchBoxApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task List_ExcludesClosedOrders_AndOrdersByQueuePriority()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);
        var list = await client.GetFromJsonAsync<PagedResult<WorkOrderDto>>(
            "/api/v1/work-orders?pageSize=500", IntegrationTestHelpers.JsonOptions);

        list.Should().NotBeNull();
        list!.Items.Should().NotBeEmpty();
        list.Items.Should().OnlyContain(w =>
            w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Delivered);

        var priorities = list.Items.Select(w => w.Status.QueuePriority()).ToList();
        priorities.Should().BeInAscendingOrder();

        var closed = await client.GetFromJsonAsync<PagedResult<WorkOrderDto>>(
            "/api/v1/work-orders?includeClosed=true&pageSize=500", IntegrationTestHelpers.JsonOptions);
        closed!.Items.Should().Contain(w =>
            w.Status == WorkOrderStatus.Completed || w.Status == WorkOrderStatus.Delivered);
    }

    [SkippableFact]
    public async Task StatusEndpoint_ReturnsPortugueseLabel()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);
        var list = await client.GetFromJsonAsync<PagedResult<WorkOrderDto>>(
            "/api/v1/work-orders?pageSize=20", IntegrationTestHelpers.JsonOptions);
        var order = list!.Items.First();

        var status = await client.GetFromJsonAsync<WorkOrderStatusDto>(
            $"/api/v1/work-orders/{order.Id}/status", IntegrationTestHelpers.JsonOptions);

        status!.Id.Should().Be(order.Id);
        status.StatusLabel.Should().Be(order.Status.ToPortuguese());
    }

    [SkippableFact]
    public async Task BudgetDecision_Reject_ReturnsToDiagnosis()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);
        var (workOrderId, token) = await CreateAwaitingApprovalAsync(client, "52998224725", "REJ1A23");

        var trackingClient = _factory.CreateClient();
        trackingClient.DefaultRequestHeaders.Add("X-Tracking-Token", token);
        var response = await trackingClient.PostAsJsonAsync(
            "/api/v1/tracking/work-orders/decision",
            new { approved = false });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TrackingWorkOrderDto>(IntegrationTestHelpers.JsonOptions);
        result!.Status.Should().Be(WorkOrderStatus.InDiagnosis);
        result.StatusLabel.Should().Be("Diagnóstico");

        var status = await client.GetFromJsonAsync<WorkOrderStatusDto>(
            $"/api/v1/work-orders/{workOrderId}/status", IntegrationTestHelpers.JsonOptions);
        status!.Status.Should().Be(WorkOrderStatus.InDiagnosis);
    }

    [SkippableFact]
    public async Task Webhook_UpdatesStatus_WithSecret()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory);
        var workOrderId = await CreateReceivedAsync(client, "71428793860", "WHK2B34");

        var webhookClient = _factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("X-Webhook-Secret", "test-webhook-secret");
        var response = await webhookClient.PostAsJsonAsync("/api/v1/webhooks/work-order-status", new
        {
            workOrderId,
            action = "start-diagnosis"
        });

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<WorkOrderDto>(IntegrationTestHelpers.JsonOptions);
        updated!.Status.Should().Be(WorkOrderStatus.InDiagnosis);

        var unauthorized = _factory.CreateClient();
        (await unauthorized.PostAsJsonAsync("/api/v1/webhooks/work-order-status", new
        {
            workOrderId,
            action = "complete"
        })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Health_Endpoints_ReturnOk()
    {
        Skip.IfNot(_factory.IsReady, _factory.SkipReason);

        var client = _factory.CreateClient();
        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<Guid> CreateReceivedAsync(HttpClient client, string document, string plate)
    {
        var services = await client.GetFromJsonAsync<PagedResult<ServiceDto>>("/api/v1/services?pageSize=500", IntegrationTestHelpers.JsonOptions);
        var service = services!.Items.First(s => s.Name == "Troca de Óleo");

        var createResponse = await client.PostAsJsonAsync("/api/v1/work-orders", new
        {
            customerDocument = document,
            customerName = "Phase2 Customer",
            customerEmail = "phase2@test.com",
            customerPhone = "11900001111",
            vehiclePlate = plate,
            vehicleBrand = "Fiat",
            vehicleModel = "Argo",
            vehicleYear = 2022,
            services = new[] { new { serviceId = service.Id, quantity = 1 } },
            parts = Array.Empty<object>(),
            notes = "Phase 2 test"
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WorkOrderDto>(IntegrationTestHelpers.JsonOptions);
        return created!.Id;
    }

    private static async Task<(Guid Id, string Token)> CreateAwaitingApprovalAsync(
        HttpClient client,
        string document,
        string plate)
    {
        var id = await CreateReceivedAsync(client, document, plate);
        (await client.PostAsync($"/api/v1/work-orders/{id}/start-diagnosis", null)).EnsureSuccessStatusCode();
        var budgetResponse = await client.PostAsync($"/api/v1/work-orders/{id}/send-budget", null);
        budgetResponse.EnsureSuccessStatusCode();
        var budget = await budgetResponse.Content.ReadFromJsonAsync<SendBudgetResponseDto>(IntegrationTestHelpers.JsonOptions);
        return (id, budget!.TrackingToken);
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WrenchBox.Application.DTOs;

namespace WrenchBox.Integration.Tests;

public static class IntegrationTestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(WrenchBoxApiFactory factory)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@wrenchbox.local",
            password = "Admin@123"
        });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        return login!.Token;
    }
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

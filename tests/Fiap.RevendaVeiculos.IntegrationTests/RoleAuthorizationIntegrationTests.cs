using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Sdk;

namespace Fiap.RevendaVeiculos.IntegrationTests;

[Trait("Category", "Docker")]
public sealed class RoleAuthorizationIntegrationTests
{
    private static readonly Uri UsersBaseAddress = new(Environment.GetEnvironmentVariable("USERS_API_URL") ?? "http://localhost:8181");
    private static readonly Uri SalesBaseAddress = new(Environment.GetEnvironmentVariable("SALES_API_URL") ?? "http://localhost:8180");

    [Fact]
    public async Task Complete_api_cycle_enforces_roles_and_payment_states()
    {
        using var users = new HttpClient { BaseAddress = UsersBaseAddress };
        using var sales = new HttpClient { BaseAddress = SalesBaseAddress };
        await EnsureComposeIsRunningAsync(users, sales);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var adminToken = await RegisterAndLoginAsync(users, $"admin{suffix}@example.com", "ADM");
        var buyerToken = await RegisterAndLoginAsync(users, $"buyer{suffix}@example.com", "COMPRADOR");

        var vehicle = new { brand = "Toyota", model = "Corolla", year = 2024, color = "Prata", price = 100000m };
        var adminCreate = await SendAsync(sales, HttpMethod.Post, "/api/v1/vehicles", adminToken, vehicle);
        Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);
        var created = await adminCreate.Content.ReadFromJsonAsync<VehicleResponse>();
        Assert.NotNull(created);

        var getVehicle = await SendAsync(sales, HttpMethod.Get, $"/api/v1/vehicles/{created!.Id}", buyerToken);
        Assert.Equal(HttpStatusCode.OK, getVehicle.StatusCode);
        var update = await SendAsync(sales, HttpMethod.Put, $"/api/v1/vehicles/{created.Id}", adminToken, new { brand = "Toyota", model = "Corolla XEi", year = 2024, color = "Preto", price = 101000m });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var available = await SendAsync(sales, HttpMethod.Get, "/api/v1/vehicles/available?page=1&pageSize=20", buyerToken);
        Assert.Equal(HttpStatusCode.OK, available.StatusCode);

        var buyerCreate = await SendAsync(sales, HttpMethod.Post, "/api/v1/vehicles", buyerToken, vehicle);
        Assert.Equal(HttpStatusCode.Forbidden, buyerCreate.StatusCode);

        var purchase = await SendAsync(sales, HttpMethod.Post, $"/api/v1/vehicles/{created!.Id}/purchases", buyerToken, new { paymentCode = $"payment-{suffix}" });
        Assert.Equal(HttpStatusCode.Created, purchase.StatusCode);

        var paid = await SendWebhookAsync(sales, $"payment-{suffix}", "PAID");
        Assert.Equal(HttpStatusCode.NoContent, paid.StatusCode);
        var sold = await SendAsync(sales, HttpMethod.Get, "/api/v1/vehicles/sold?page=1&pageSize=20", buyerToken);
        Assert.Equal(HttpStatusCode.OK, sold.StatusCode);

        var secondCreate = await SendAsync(sales, HttpMethod.Post, "/api/v1/vehicles", adminToken, new { brand = "Honda", model = "Civic", year = 2024, color = "Branco", price = 150000m });
        Assert.Equal(HttpStatusCode.Created, secondCreate.StatusCode);
        var secondVehicle = await secondCreate.Content.ReadFromJsonAsync<VehicleResponse>();
        var canceledCode = $"cancel-{suffix}";
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(sales, HttpMethod.Post, $"/api/v1/vehicles/{secondVehicle!.Id}/purchases", buyerToken, new { paymentCode = canceledCode })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendWebhookAsync(sales, canceledCode, "CANCELED")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(sales, HttpMethod.Get, $"/api/v1/vehicles/{secondVehicle.Id}", buyerToken)).StatusCode);
    }

    private static async Task EnsureComposeIsRunningAsync(HttpClient users, HttpClient sales)
    {
        try
        {
            var checks = await Task.WhenAll(users.GetAsync("/health/live"), sales.GetAsync("/health/live"));
            if (checks.All(response => response.IsSuccessStatusCode)) return;
        }
        catch (HttpRequestException)
        {
            // The actionable message below is clearer than the connection exception.
        }

        throw new XunitException($"As APIs de integração não estão disponíveis em {UsersBaseAddress} e {SalesBaseAddress}. " +
            "Inicie o ambiente antes do teste: docker compose -f compose.yaml -f compose.local-ports.yaml up -d --build");
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string role)
    {
        var cpf = Random.Shared.NextInt64(10_000_000_000L, 99_999_999_999L).ToString();
        var register = await client.PostAsJsonAsync("/api/v1/users", new { name = role, cpf, email, password = "Senha@123", role });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Senha@123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var response = await login.Content.ReadFromJsonAsync<LoginResponse>();
        return Assert.IsType<string>(response?.AccessToken);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string token, object body)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendWebhookAsync(HttpClient client, string paymentCode, string status)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook") { Content = JsonContent.Create(new { paymentCode, status }) };
        request.Headers.Add("X-Webhook-Secret", Environment.GetEnvironmentVariable("WEBHOOK_SECRET") ?? "change-me-webhook-secret");
        return client.SendAsync(request);
    }

    private sealed record LoginResponse(string AccessToken);
    private sealed record VehicleResponse(string Id);
}

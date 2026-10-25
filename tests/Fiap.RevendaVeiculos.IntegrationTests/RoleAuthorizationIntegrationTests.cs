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
        var createdResponse = await SendAsync(sales, HttpMethod.Post, "/api/v1/vehicles", adminToken, vehicle);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        Assert.NotNull(created);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(sales, HttpMethod.Get, $"/api/v1/vehicles/{created!.Id}", buyerToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(sales, HttpMethod.Put, $"/api/v1/vehicles/{created.Id}", adminToken, new { brand = "Toyota", model = "Corolla XEi", year = 2024, color = "Preto", price = 101000m })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(sales, HttpMethod.Get, "/api/v1/vehicles/available?page=1&pageSize=20", buyerToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(sales, HttpMethod.Post, "/api/v1/vehicles", buyerToken, vehicle)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(sales, HttpMethod.Post, $"/api/v1/vehicles/{created.Id}/purchases", buyerToken, new { paymentCode = $"payment-{suffix}" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendWebhookAsync(sales, $"payment-{suffix}", "PAID")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(sales, HttpMethod.Get, "/api/v1/vehicles/sold?page=1&pageSize=20", buyerToken)).StatusCode);
        var second = await SendAsync(sales, HttpMethod.Post, "/api/v1/vehicles", adminToken, new { brand = "Honda", model = "Civic", year = 2024, color = "Branco", price = 150000m });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondVehicle = await second.Content.ReadFromJsonAsync<VehicleResponse>();
        var canceledCode = $"cancel-{suffix}";
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(sales, HttpMethod.Post, $"/api/v1/vehicles/{secondVehicle!.Id}/purchases", buyerToken, new { paymentCode = canceledCode })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendWebhookAsync(sales, canceledCode, "CANCELED")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(sales, HttpMethod.Get, $"/api/v1/vehicles/{secondVehicle.Id}", buyerToken)).StatusCode);
    }

    private static async Task EnsureComposeIsRunningAsync(HttpClient users, HttpClient sales)
    {
        try { if ((await users.GetAsync("/health/live")).IsSuccessStatusCode && (await users.GetAsync("/health/ready")).IsSuccessStatusCode && (await sales.GetAsync("/health/live")).IsSuccessStatusCode && (await sales.GetAsync("/health/ready")).IsSuccessStatusCode) return; }
        catch (HttpRequestException) { }
        throw new XunitException("APIs indisponíveis. Inicie: docker compose -f compose.yaml -f compose.local-ports.yaml up -d --build");
    }
    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string role)
    {
        var cpf = Random.Shared.NextInt64(10_000_000_000L, 99_999_999_999L).ToString();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/users", new { name = role, cpf, email, password = "Senha@123", role })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Senha@123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return Assert.IsType<string>((await login.Content.ReadFromJsonAsync<LoginResponse>())?.AccessToken);
    }
    private static Task<HttpResponseMessage> SendAsync(HttpClient c,HttpMethod m,string p,string t,object b) { var r=new HttpRequestMessage(m,p){Content=JsonContent.Create(b)};r.Headers.Authorization=new("Bearer",t);return c.SendAsync(r); }
    private static Task<HttpResponseMessage> SendAsync(HttpClient c,HttpMethod m,string p,string t) { var r=new HttpRequestMessage(m,p);r.Headers.Authorization=new("Bearer",t);return c.SendAsync(r); }
    private static Task<HttpResponseMessage> SendWebhookAsync(HttpClient c,string code,string status) { var r=new HttpRequestMessage(HttpMethod.Post,"/api/v1/payments/webhook"){Content=JsonContent.Create(new { paymentCode=code,status })};r.Headers.Add("X-Webhook-Secret",Environment.GetEnvironmentVariable("WEBHOOK_SECRET")??"change-me-webhook-secret");return c.SendAsync(r); }
    private sealed record LoginResponse(string AccessToken); private sealed record VehicleResponse(string Id);
}

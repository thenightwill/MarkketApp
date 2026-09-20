using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Authentication;
using Application.Inventory;
using Application.Products;
using Domain.Enums;

namespace Market.Tests.Api;

public static class ApiHelpers
{
    public static readonly JsonSerializerOptions Json = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public sealed record Problem(int Status, string? Title, string? Detail, string? Code);

    public sealed record AuthenticatedClient(HttpClient Client, Guid UserId);

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, Json);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new InvalidOperationException("Empty response body.");
    }

    public static async Task<string?> CodeAsync(this HttpResponseMessage response) =>
        (await response.ReadAsync<Problem>()).Code;

    public static HttpClient CreateAnonymousClient() => ApiFixture.Factory.CreateClient();

    public static async Task<AuthenticatedClient> LoginAsync(string email, string password)
    {
        var client = CreateAnonymousClient();
        var response = await client.PostJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var auth = await response.ReadAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new AuthenticatedClient(client, auth.User.Id);
    }

    public static Task<AuthenticatedClient> AdminAsync() =>
        LoginAsync("admin@supermarket.local", ApiFixture.AdminPassword);

    public static Task<AuthenticatedClient> SeededEmployeeAsync() =>
        LoginAsync("employee1@supermarket.local", ApiFixture.EmployeePassword);

    public static async Task<AuthenticatedClient> NewEmployeeAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@supermarket.local";
        var client = CreateAnonymousClient();
        var response = await client.PostJsonAsync("/api/auth/register", new RegisterRequest("Test User", email, "Secret123"));
        response.EnsureSuccessStatusCode();

        var auth = await response.ReadAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new AuthenticatedClient(client, auth.User.Id);
    }

    public static async Task<ProductResponse> CreateProductAsync(
        HttpClient admin,
        UnitType unitType = UnitType.Volume,
        decimal cost = 2500m,
        decimal salePrice = 3200m)
    {
        var response = await admin.PostJsonAsync(
            "/api/products",
            new CreateProductRequest($"Producto {Guid.NewGuid():N}", "Marca", "Categoria", unitType, 1m, cost, salePrice));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ProductResponse>();
    }

    public static async Task<StockResponse> CreateStockAsync(
        HttpClient admin,
        Guid productId,
        string batch,
        decimal quantity,
        int expiresInDays = 30,
        decimal minimumStock = 1m)
    {
        var now = DateTime.UtcNow;
        var response = await admin.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(productId, batch, "A-01", quantity, minimumStock, now.AddDays(-60), now.AddDays(expiresInDays)));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<StockResponse>();
    }
}

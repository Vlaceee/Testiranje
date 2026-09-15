using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace ForgeMart.PerformanceTests;

[TestFixture]
[Category("Performance")]
[NonParallelizable]
public sealed class EducationalLoadTests
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("FORGEMART_API_URL") ?? "http://localhost:5073"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    [Test]
    public async Task ProductListing_OneHundredConcurrentRequests_CompleteWithoutServerErrors()
    {
        var stopwatch = Stopwatch.StartNew();
        var responses = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Client.GetAsync("/api/v1/products?page=1&pageSize=20")));
        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(responses, Has.All.Property(nameof(HttpResponseMessage.StatusCode)).EqualTo(HttpStatusCode.OK));
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
        });
    }

    [Test]
    public async Task SearchAndFilter_FiftyConcurrentRequests_CompleteWithoutServerErrors()
    {
        var queries = Enumerable.Range(0, 50).Select(index =>
            index % 2 == 0
                ? "/api/v1/products?search=drill&inStock=true&page=1&pageSize=12"
                : "/api/v1/products?brand=VoltCraft&discounted=false&page=1&pageSize=12");
        var stopwatch = Stopwatch.StartNew();
        var responses = await Task.WhenAll(queries.Select(Client.GetAsync));
        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(responses, Has.All.Property(nameof(HttpResponseMessage.StatusCode)).EqualTo(HttpStatusCode.OK));
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
        });
    }

    [Test]
    public async Task Checkout_TwentyConcurrentIndependentCustomers_CompleteWithoutServerErrors()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var productId = await GetWellStockedProductIdAsync();
        await RestockAsync(productId, 100m);
        var tokens = await Task.WhenAll(Enumerable.Range(1, 20).Select(index => RegisterAsync($"perf-{suffix}-{index}@forgemart.test")));
        await Task.WhenAll(tokens.Select(token => PutCartItemAsync(token, productId)));

        var stopwatch = Stopwatch.StartNew();
        var responses = await Task.WhenAll(tokens.Select(CheckoutAsync));
        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(responses, Has.All.Property(nameof(HttpResponseMessage.StatusCode)).EqualTo(HttpStatusCode.Created));
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
        });
    }

    private static async Task<Guid> GetWellStockedProductIdAsync()
    {
        using var response = await Client.GetAsync("/api/v1/products?inStock=true&page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    private static async Task RestockAsync(Guid productId, decimal quantity)
    {
        var token = await LoginAsync("admin@forgemart.test", "Admin123!");
        using var request = Authorized(HttpMethod.Post, $"/api/v1/admin/inventory/{productId}/adjust", token);
        request.Content = JsonContent.Create(new { quantity, type = "Restock", reason = "Educational performance test setup" });
        using var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> RegisterAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Performance123!",
            firstName = "Load",
            lastName = "Tester"
        });
        response.EnsureSuccessStatusCode();
        return await ReadTokenAsync(response);
    }

    private static async Task<string> LoginAsync(string email, string password)
    {
        using var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return await ReadTokenAsync(response);
    }

    private static async Task PutCartItemAsync(string token, Guid productId)
    {
        using var request = Authorized(HttpMethod.Put, $"/api/v1/cart/items/{productId}", token);
        request.Content = JsonContent.Create(new { quantity = 1 });
        using var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> CheckoutAsync(string token)
    {
        using var request = Authorized(HttpMethod.Post, "/api/v1/orders/checkout", token);
        request.Content = JsonContent.Create(new
        {
            contactName = "Load Tester",
            contactEmail = "performance@forgemart.test",
            shippingAddress = "Performance Street 1",
            shippingCity = "Nis",
            shippingPostalCode = "18000",
            paymentMethod = "CashOnDelivery",
            mockCardNumber = (string?)null
        });
        return await Client.SendAsync(request);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
}

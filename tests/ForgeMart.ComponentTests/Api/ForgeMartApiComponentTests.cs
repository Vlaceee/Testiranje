using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.ComponentTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeMart.ComponentTests.Api;

[TestFixture]
[Category("Component")]
[NonParallelizable]
public sealed class ForgeMartApiComponentTests : IDisposable
{
    private ForgeMartApiFactory factory = null!;
    private HttpClient client = null!;

    [OneTimeSetUp]
    public void CreateFactory()
    {
        factory = new ForgeMartApiFactory();
        client = factory.CreateClient();
    }

    [SetUp]
    public async Task ResetDatabase()
    {
        client.DefaultRequestHeaders.Authorization = null;
        await factory.ResetAsync();
    }

    [OneTimeTearDown]
    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    [Test]
    public async Task Health_ReturnsHealthy()
    {
        var response = await client.GetAsync("/health");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.ReadAsStringAsync().Result, Is.EqualTo("Healthy"));
        });
    }

    [Test]
    public async Task SystemInfo_ReturnsVersionedIdentity()
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/v1/system/info");
        Assert.Multiple(() =>
        {
            Assert.That(response.GetProperty("name").GetString(), Is.EqualTo("ForgeMart"));
            Assert.That(response.GetProperty("apiVersion").GetString(), Is.EqualTo("v1"));
        });
    }

    [Test]
    public async Task Register_ValidCustomer_ReturnsCreatedAndJwt()
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "new.customer@forgemart.test", password = "Secure123!", firstName = "Ana", lastName = "Ilic"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(auth!.AccessToken, Is.Not.Empty);
            Assert.That(auth.User.Role, Is.EqualTo("Customer"));
        });
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "customer@forgemart.test", password = "Secure123!", firstName = "Ana", lastName = "Ilic"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [TestCase("password")]
    [TestCase("Password1")]
    [TestCase("PASSWORD1!")]
    [TestCase("password1!")]
    public async Task Register_WeakPassword_ReturnsBadRequest(string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "new.customer@forgemart.test", password, firstName = "Ana", lastName = "Ilic"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Login_ValidAdmin_ReturnsAdminClaims()
    {
        var auth = await LoginAsync("admin@forgemart.test", "Admin123!");
        Assert.That(auth.User.Role, Is.EqualTo("Admin"));
    }

    [TestCase("admin@forgemart.test", "wrong")]
    [TestCase("missing@forgemart.test", "Admin123!")]
    public async Task Login_InvalidCredentials_ReturnUnauthorized(string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_DisabledCustomer_ReturnsForbidden()
    {
        await WithDbAsync(async db =>
        {
            var user = db.Users.Single(candidate => candidate.Email == "customer@forgemart.test");
            user.IsActive = false;
            await db.SaveChangesAsync();
        });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "customer@forgemart.test", password = "Customer123!"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Products_AnonymousList_HidesInactiveProducts()
    {
        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products?page=1&pageSize=100");
        Assert.Multiple(() =>
        {
            Assert.That(result!.TotalCount, Is.EqualTo(38));
            Assert.That(result.Items, Has.All.Matches<ProductResponse>(product => product.IsActive));
        });
    }

    [TestCase("drill", "18V Cordless Drill")]
    [TestCase("DRILL", "18V Cordless Drill")]
    [TestCase("fm-01-001", "18V Cordless Drill")]
    [TestCase("voltcraft", "125mm Angle Grinder")]
    [TestCase("craftspeople", "18V Cordless Drill")]
    public async Task Products_Search_IsPartialAndCaseInsensitive(string search, string expectedFirst)
    {
        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/v1/products?page=1&pageSize=100&search={Uri.EscapeDataString(search)}");
        Assert.That(result!.Items.Select(product => product.Name), Does.Contain(expectedFirst));
    }

    [TestCase("page=0&pageSize=20")]
    [TestCase("page=1&pageSize=0")]
    [TestCase("page=1&pageSize=101")]
    [TestCase("page=-1&pageSize=20")]
    public async Task Products_InvalidPagination_ReturnsBadRequest(string query)
    {
        var response = await client.GetAsync($"/api/v1/products?{query}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Products_PageSizeFive_ReturnsFiveAndMetadata()
    {
        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products?page=2&pageSize=5");
        Assert.Multiple(() =>
        {
            Assert.That(result!.Items, Has.Count.EqualTo(5));
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.TotalPages, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task Products_InStockFilter_ReturnsOnlyPositiveStock()
    {
        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products?page=1&pageSize=100&inStock=true");
        Assert.That(result!.Items, Has.All.Matches<ProductResponse>(product => product.StockQuantity > 0));
    }

    [Test]
    public async Task Products_DiscountFilter_ReturnsOnlyActiveDiscounts()
    {
        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products?page=1&pageSize=100&discounted=true");
        Assert.That(result!.Items, Has.All.Matches<ProductResponse>(product => product.HasActiveDiscount));
    }

    [Test]
    public async Task Products_GetMissing_ReturnsNotFound()
    {
        var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Products_GetExisting_ReturnsDetailedSnapshot()
    {
        var id = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var product = await client.GetFromJsonAsync<ProductDetailResponse>($"/api/v1/products/{id}");
        Assert.Multiple(() =>
        {
            Assert.That(product!.Name, Is.EqualTo("18V Cordless Drill"));
            Assert.That(product.Sku, Is.EqualTo("FM-01-001"));
            Assert.That(product.StockQuantity, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Products_GetInactive_AsCustomer_ReturnsNotFound()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000039");
        var response = await client.GetAsync($"/api/v1/products/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ProductCreate_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", await ValidProductRequestAsync());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ProductCreate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PostAsJsonAsync("/api/v1/products", await ValidProductRequestAsync());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ProductCreate_AdminValid_ReturnsCreatedAndPersists()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var response = await client.PostAsJsonAsync("/api/v1/products", await ValidProductRequestAsync());
        var product = await response.Content.ReadFromJsonAsync<ProductDetailResponse>();
        var get = await client.GetAsync($"/api/v1/products/{product!.Id}");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(product.StockQuantity, Is.EqualTo(12));
            Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task ProductCreate_DuplicateSku_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var request = await ValidProductRequestAsync("FM-01-001");
        var response = await client.PostAsJsonAsync("/api/v1/products", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [TestCase(0, 50)]
    [TestCase(-1, 0)]
    [TestCase(100, 101)]
    public async Task ProductCreate_InvalidMoney_ReturnsBadRequest(decimal price, decimal cost)
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var categoryId = await FirstCategoryIdAsync();
        var response = await client.PostAsJsonAsync("/api/v1/products", ProductPayload(categoryId, "NEW-SKU-001", price, cost));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ProductUpdate_AdminValid_PersistsChanges()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var categoryId = await FirstCategoryIdAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/products/{id}", ProductPayload(categoryId, "UPDATED-001", 1500m, 800m));
        var updated = await response.Content.ReadFromJsonAsync<ProductDetailResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(updated!.Sku, Is.EqualTo("UPDATED-001"));
            Assert.That(updated.Price, Is.EqualTo(1500m));
        });
    }

    [Test]
    public async Task ProductUpdate_InvalidPrice_ReturnsBadRequestAndKeepsOldValue()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var categoryId = await FirstCategoryIdAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/products/{id}", ProductPayload(categoryId, "FM-01-001", -1m, 0m));
        await WithDbAsync(db =>
        {
            Assert.That(db.Products.Single(product => product.Id == id).Price, Is.EqualTo(249m));
            return Task.CompletedTask;
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ProductUpdate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.PutAsJsonAsync($"/api/v1/products/{id}", ProductPayload(await FirstCategoryIdAsync(), "UPDATED-001", 1500m, 800m));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ProductDeactivate_Admin_ReturnsNoContentAndHidesProduct()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.DeleteAsync($"/api/v1/products/{id}");
        client.DefaultRequestHeaders.Authorization = null;
        var get = await client.GetAsync($"/api/v1/products/{id}");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task ProductDeactivate_AlreadyInactive_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000039");
        var response = await client.DeleteAsync($"/api/v1/products/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task ProductDeactivate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var id = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.DeleteAsync($"/api/v1/products/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Categories_AnonymousList_ReturnsAllActiveSeedCategories()
    {
        var result = await client.GetFromJsonAsync<CategoryResponse[]>("/api/v1/categories");
        Assert.That(result, Has.Length.EqualTo(8));
    }

    [Test]
    public async Task CategoryCreate_AdminValid_ReturnsCreated()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "Garden Tools", description = "Outdoor tools and practical garden equipment"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task CategoryCreate_DuplicateCaseInsensitive_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "power tools", description = "Duplicate category description"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task CategoryCreate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "Garden Tools", description = "Outdoor tools and practical garden equipment"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task CategoryUpdate_AdminValid_PersistsName()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var response = await client.PutAsJsonAsync($"/api/v1/categories/{id}", new
        {
            name = "Professional Power Tools", description = "Updated deterministic category description"
        });
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(category!.Name, Is.EqualTo("Professional Power Tools"));
        });
    }

    [Test]
    public async Task CategoryUpdate_DuplicateName_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var response = await client.PutAsJsonAsync($"/api/v1/categories/{id}", new
        {
            name = "Hand Tools", description = "Duplicate deterministic category description"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task CategoryUpdate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var response = await client.PutAsJsonAsync($"/api/v1/categories/{id}", new
        {
            name = "Professional Power Tools", description = "Updated deterministic category description"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task CategoryDeactivate_AdminValid_HidesCategoryFromPublicList()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var response = await client.DeleteAsync($"/api/v1/categories/{id}");
        client.DefaultRequestHeaders.Authorization = null;
        var categories = await client.GetFromJsonAsync<CategoryResponse[]>("/api/v1/categories");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(categories!.Select(category => category.Id), Does.Not.Contain(id));
        });
    }

    [Test]
    public async Task CategoryDeactivate_AlreadyInactive_ReturnsConflict()
    {
        var id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        await WithDbAsync(async db =>
        {
            db.Categories.Single(category => category.Id == id).IsActive = false;
            await db.SaveChangesAsync();
        });
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var response = await client.DeleteAsync($"/api/v1/categories/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task CategoryDeactivate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var response = await client.DeleteAsync($"/api/v1/categories/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Cart_GetAuthenticatedCustomer_ReturnsEmptySeedCart()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var cart = await client.GetFromJsonAsync<CartResponse>("/api/v1/cart");
        Assert.That(cart!.Items, Is.Empty);
    }

    [Test]
    public async Task Cart_SetValidItem_PersistsQuantity()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 2 });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.That(cart!.Items.Single().Quantity, Is.EqualTo(2));
    }

    [Test]
    public async Task Cart_SetBeyondStock_ReturnsConflict()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000011");
        var response = await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 2 });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Cart_MergeCombinesDuplicateWithServerQuantity()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 1 });
        var response = await client.PostAsJsonAsync("/api/v1/cart/merge", new
        {
            items = new[] { new { productId, quantity = 2 } }
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.That(cart!.Items.Single().Quantity, Is.EqualTo(3));
    }

    [Test]
    public async Task Cart_RemoveMissing_ReturnsNotFound()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.DeleteAsync($"/api/v1/cart/items/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Checkout_CashOnDelivery_CreatesPendingOrderAndDeductsStock()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 2 });
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", CheckoutPayload("CashOnDelivery", null));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(order!.Status, Is.EqualTo(OrderStatus.PendingPayment));
            Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Pending));
            Assert.That(order.Items.Single().Quantity, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Checkout_SuccessCard_CreatesPaidOrder()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 1 });
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", CheckoutPayload("MockCard", "4111111111111111"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(order!.Status, Is.EqualTo(OrderStatus.Paid));
            Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
            Assert.That(order.PaidAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task Checkout_DeclinedCard_DoesNotCreateOrder()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var before = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 1 });
        var response = await client.PostAsJsonAsync("/api/v1/orders/checkout", CheckoutPayload("MockCard", "4000000000000002"));
        var orders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(orders!.TotalCount, Is.EqualTo(before!.TotalCount));
        });
    }

    [Test]
    public async Task CustomerCancel_PaidOrder_ReturnsConflict()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 1 });
        var checkout = await client.PostAsJsonAsync("/api/v1/orders/checkout", CheckoutPayload("MockCard", "4111111111111111"));
        var order = await checkout.Content.ReadFromJsonAsync<OrderResponse>();
        var response = await client.PostAsync($"/api/v1/orders/{order!.Id}/cancel", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task CustomerCancel_UnpaidPendingOrder_ReturnsCancelled()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{productId}", new { quantity = 1 });
        var checkout = await client.PostAsJsonAsync("/api/v1/orders/checkout", CheckoutPayload("CashOnDelivery", null));
        var order = await checkout.Content.ReadFromJsonAsync<OrderResponse>();
        var response = await client.PostAsync($"/api/v1/orders/{order!.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.That(cancelled!.Status, Is.EqualTo(OrderStatus.Cancelled));
    }

    [TestCase(0, 20)]
    [TestCase(1, 0)]
    [TestCase(1, 101)]
    public async Task Orders_ListInvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.GetAsync($"/api/v1/orders?page={page}&pageSize={pageSize}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Orders_GetMissing_ReturnsNotFound()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Orders_GetAnotherCustomersOrder_ReturnsNotFound()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var ownOrders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders?page=1&pageSize=20");
        client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateAsync("marko@forgemart.test", "Customer123!");
        var response = await client.GetAsync($"/api/v1/orders/{ownOrders!.Items[0].Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CustomerCancel_AnotherCustomersOrder_ReturnsNotFound()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var orders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders?page=1&pageSize=20");
        var pending = orders!.Items.Single(order => order.OrderNumber == "FM-SEED-PENDING");
        client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateAsync("marko@forgemart.test", "Customer123!");
        var response = await client.PostAsync($"/api/v1/orders/{pending.Id}/cancel", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AdminOrderStatus_ValidPath_SetsPaidShippedAndDeliveredTimestamps()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var orders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders?page=1&pageSize=100");
        var id = orders!.Items.Single(order => order.OrderNumber == "FM-SEED-PENDING").Id;
        foreach (var status in new[] { "Paid", "Processing", "Shipped", "Delivered" })
        {
            var response = await client.PutAsJsonAsync($"/api/v1/admin/orders/{id}/status", new { status });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        var delivered = await client.GetFromJsonAsync<OrderResponse>($"/api/v1/orders/{id}");
        Assert.Multiple(() =>
        {
            Assert.That(delivered!.Status, Is.EqualTo(OrderStatus.Delivered));
            Assert.That(delivered.PaidAt, Is.Not.Null);
            Assert.That(delivered.ShippedAt, Is.Not.Null);
            Assert.That(delivered.DeliveredAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task AdminOrderStatus_CancelPending_RestoresStock()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var orders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders?page=1&pageSize=100");
        var pending = orders!.Items.Single(order => order.OrderNumber == "FM-SEED-PENDING");
        var productId = pending.Items.Single().ProductId;
        decimal before = 0;
        await WithDbAsync(db =>
        {
            before = db.Inventories.Single(inventory => inventory.ProductId == productId).Quantity;
            return Task.CompletedTask;
        });
        var response = await client.PutAsJsonAsync($"/api/v1/admin/orders/{pending.Id}/status", new { status = "Cancelled" });
        await WithDbAsync(db =>
        {
            Assert.That(db.Inventories.Single(inventory => inventory.ProductId == productId).Quantity, Is.EqualTo(before + 2));
            return Task.CompletedTask;
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AdminOrderStatus_InvalidTransition_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var orders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders?page=1&pageSize=100");
        var delivered = orders!.Items.Single(order => order.OrderNumber == "FM-SEED-DELIVERED");
        var response = await client.PutAsJsonAsync($"/api/v1/admin/orders/{delivered.Id}/status", new { status = "Paid" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task InventoryAdjust_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.PostAsJsonAsync($"/api/v1/admin/inventory/{productId}/adjust", new
        {
            quantity = 10, type = "Restock", reason = "Component test", increase = true
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task InventoryAdjust_AdminRestock_CreatesMovementAndIncreasesStock()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.PostAsJsonAsync($"/api/v1/admin/inventory/{productId}/adjust", new
        {
            quantity = 10, type = "Restock", reason = "Component test", increase = true
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var inventory = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        var history = await client.GetFromJsonAsync<StockMovementResponse[]>($"/api/v1/admin/inventory/{productId}/movements");
        Assert.Multiple(() =>
        {
            Assert.That(inventory!.Quantity, Is.EqualTo(12));
            Assert.That(history![0].QuantityChange, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task InventoryAdjust_BelowZero_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var productId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var response = await client.PostAsJsonAsync($"/api/v1/admin/inventory/{productId}/adjust", new
        {
            quantity = 3, type = "Damaged", reason = "Component test", increase = false
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task DisableFinalAdministrator_ReturnsConflict()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var adminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{adminId}/active", new { isActive = false });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Dashboard_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.GetAsync("/api/v1/admin/dashboard");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Dashboard_Admin_ReturnsSeedLowStockMetrics()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var response = await client.GetFromJsonAsync<DashboardResponse>("/api/v1/admin/dashboard");
        Assert.Multiple(() =>
        {
            Assert.That(response!.LowStockProducts, Has.Count.EqualTo(6));
            Assert.That(response.TotalRevenue, Is.GreaterThan(0));
        });
    }

    private async Task<Guid> FirstCategoryIdAsync()
    {
        var categories = await client.GetFromJsonAsync<CategoryResponse[]>("/api/v1/categories");
        return categories![0].Id;
    }

    private async Task<object> ValidProductRequestAsync(string sku = "NEW-SKU-001") =>
        ProductPayload(await FirstCategoryIdAsync(), sku, 1000m, 600m);

    private static object ProductPayload(Guid categoryId, string sku, decimal price, decimal cost) => new
    {
        name = "Component Test Drill", sku, description = "A deterministic component test product.", brand = "TestForge",
        categoryId, price, costPrice = cost, vatRate = 20, discountPercentage = 0,
        unitOfMeasure = "Piece", unitsPerPackage = 1, imageUrl = "/assets/products/tool-placeholder.svg",
        initialStockQuantity = 12, minimumStockLevel = 3
    };

    private static object CheckoutPayload(string paymentMethod, string? card) => new
    {
        contactName = "Mila Petrovic", contactEmail = "customer@forgemart.test", shippingAddress = "Test Street 1",
        shippingCity = "Nis", shippingPostalCode = "18000", paymentMethod, mockCardNumber = card
    };

    private static readonly Guid FirstProductId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid InactiveProductId = Guid.Parse("40000000-0000-0000-0000-000000000039");
    private static readonly Guid FirstCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid LastCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000008");
    private static readonly Guid CustomerId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid MarkoId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    [Test]
    public async Task AuthMe_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AuthMe_AuthenticatedCustomer_ReturnsCurrentCustomer()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var user = await client.GetFromJsonAsync<CurrentUserResponse>("/api/v1/auth/me");
        Assert.Multiple(() =>
        {
            Assert.That(user!.Id, Is.EqualTo(CustomerId));
            Assert.That(user.Email, Is.EqualTo("customer@forgemart.test"));
            Assert.That(user.Role, Is.EqualTo("Customer"));
            Assert.That(user.IsActive, Is.True);
        });
    }

    [Test]
    public async Task AuthMe_AuthenticatedAdmin_ReturnsAdminRole()
    {
        await AuthenticateAsync("admin@forgemart.test", "Admin123!");
        var user = await client.GetFromJsonAsync<CurrentUserResponse>("/api/v1/auth/me");
        Assert.That(user!.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task ProductReactivate_InactiveProduct_ReturnsNoContentAndPersists()
    {
        await AuthenticateAdminAsync();
        var response = await client.PostAsync($"/api/v1/products/{InactiveProductId}/reactivate", null);
        var isActive = await ReadDbAsync(db => db.Products.Single(product => product.Id == InactiveProductId).IsActive);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(isActive, Is.True);
        });
    }

    [Test]
    public async Task ProductReactivate_AlreadyActiveProduct_ReturnsConflict()
    {
        await AuthenticateAdminAsync();
        var response = await client.PostAsync($"/api/v1/products/{FirstProductId}/reactivate", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task ProductReactivate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PostAsync($"/api/v1/products/{InactiveProductId}/reactivate", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Categories_AnonymousList_HidesInactiveCategory()
    {
        await SetCategoryActiveAsync(LastCategoryId, false);
        var categories = await client.GetFromJsonAsync<CategoryResponse[]>("/api/v1/categories?includeInactive=true");
        Assert.That(categories!.Select(category => category.Id), Does.Not.Contain(LastCategoryId));
    }

    [Test]
    public async Task Categories_AdminList_CanIncludeInactiveCategory()
    {
        await SetCategoryActiveAsync(LastCategoryId, false);
        await AuthenticateAdminAsync();
        var categories = await client.GetFromJsonAsync<CategoryResponse[]>("/api/v1/categories?includeInactive=true");
        var category = categories!.Single(candidate => candidate.Id == LastCategoryId);
        Assert.That(category.IsActive, Is.False);
    }

    [Test]
    public async Task CategoryReactivate_InactiveCategory_ReturnsNoContentAndPersists()
    {
        await SetCategoryActiveAsync(LastCategoryId, false);
        await AuthenticateAdminAsync();
        var response = await client.PostAsync($"/api/v1/categories/{LastCategoryId}/reactivate", null);
        var isActive = await ReadDbAsync(db => db.Categories.Single(category => category.Id == LastCategoryId).IsActive);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(isActive, Is.True);
        });
    }

    [Test]
    public async Task CategoryReactivate_AlreadyActiveCategory_ReturnsConflict()
    {
        await AuthenticateAdminAsync();
        var response = await client.PostAsync($"/api/v1/categories/{FirstCategoryId}/reactivate", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task CategoryReactivate_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PostAsync($"/api/v1/categories/{LastCategoryId}/reactivate", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task CartGet_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await client.GetAsync("/api/v1/cart");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CartGet_SecondCustomer_HasIndependentEmptyCart()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{FirstProductId}", new { quantity = 1 });
        await AuthenticateAsync("marko@forgemart.test", "Customer123!");
        var cart = await client.GetFromJsonAsync<CartResponse>("/api/v1/cart");
        Assert.That(cart!.Items, Is.Empty);
    }

    [Test]
    public async Task CartSet_UnknownProduct_ReturnsNotFound()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PutAsJsonAsync($"/api/v1/cart/items/{Guid.NewGuid()}", new { quantity = 1 });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CartDelete_ExistingItem_ReturnsEmptyCart()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        await client.PutAsJsonAsync($"/api/v1/cart/items/{FirstProductId}", new { quantity = 1 });
        var response = await client.DeleteAsync($"/api/v1/cart/items/{FirstProductId}");
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(cart!.Items, Is.Empty);
            Assert.That(cart.GrandTotal, Is.Zero);
        });
    }

    [Test]
    public async Task CartDelete_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await client.DeleteAsync($"/api/v1/cart/items/{FirstProductId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CartMerge_EmptyPayload_ReturnsBadRequest()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PostAsJsonAsync("/api/v1/cart/merge", new { items = Array.Empty<object>() });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CartMerge_DuplicateProductIds_ReturnsBadRequest()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PostAsJsonAsync("/api/v1/cart/merge", new
        {
            items = new[]
            {
                new { productId = FirstProductId, quantity = 1 },
                new { productId = FirstProductId, quantity = 1 }
            }
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task OrdersGet_OwnerCanReadSeededOrder()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var orders = await client.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/v1/orders?page=1&pageSize=20");
        var expected = orders!.Items.Single(order => order.OrderNumber == "FM-SEED-PENDING");
        var response = await client.GetAsync($"/api/v1/orders/{expected.Id}");
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(order!.UserId, Is.EqualTo(CustomerId));
            Assert.That(order.OrderNumber, Is.EqualTo("FM-SEED-PENDING"));
        });
    }

    [Test]
    public async Task InventoryList_AdminReceivesAllSeededInventoryRows()
    {
        await AuthenticateAdminAsync();
        var inventory = await client.GetFromJsonAsync<InventoryResponse[]>("/api/v1/admin/inventory");
        Assert.That(inventory, Has.Length.EqualTo(40));
    }

    [Test]
    public async Task InventoryList_LowStockFilterReturnsOnlyLowStockRows()
    {
        await AuthenticateAdminAsync();
        var inventory = await client.GetFromJsonAsync<InventoryResponse[]>("/api/v1/admin/inventory?lowStockOnly=true");
        Assert.Multiple(() =>
        {
            Assert.That(inventory, Is.Not.Empty);
            Assert.That(inventory, Has.All.Matches<InventoryResponse>(item => item.IsLowStock));
            Assert.That(inventory, Has.All.Matches<InventoryResponse>(item => item.Quantity <= item.MinimumStockLevel));
        });
    }

    [Test]
    public async Task InventoryList_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.GetAsync("/api/v1/admin/inventory");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task InventoryMovements_AdminReceivesSeedMovementHistory()
    {
        await AuthenticateAdminAsync();
        var movements = await client.GetFromJsonAsync<StockMovementResponse[]>($"/api/v1/admin/inventory/{FirstProductId}/movements");
        Assert.Multiple(() =>
        {
            Assert.That(movements, Is.Not.Empty);
            Assert.That(movements![0].Type, Is.EqualTo(StockMovementType.Restock));
            Assert.That(movements[0].QuantityChange, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task InventoryMovements_UnknownProduct_ReturnsNotFound()
    {
        await AuthenticateAdminAsync();
        var response = await client.GetAsync($"/api/v1/admin/inventory/{Guid.NewGuid()}/movements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task InventoryMovements_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.GetAsync($"/api/v1/admin/inventory/{FirstProductId}/movements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AdminUsersList_AdminReceivesAllSeededUsers()
    {
        await AuthenticateAdminAsync();
        var users = await client.GetFromJsonAsync<UserAdminResponse[]>("/api/v1/admin/users");
        Assert.Multiple(() =>
        {
            Assert.That(users, Has.Length.EqualTo(3));
            Assert.That(users!.Select(user => user.Email), Does.Contain("admin@forgemart.test"));
            Assert.That(users!.Select(user => user.Email), Does.Contain("customer@forgemart.test"));
            Assert.That(users!.Select(user => user.Email), Does.Contain("marko@forgemart.test"));
        });
    }

    [Test]
    public async Task AdminUsersList_Customer_ReturnsForbidden()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.GetAsync("/api/v1/admin/users");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AdminUsersList_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await client.GetAsync("/api/v1/admin/users");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AdminUserSetActive_UnknownUser_ReturnsNotFound()
    {
        await AuthenticateAdminAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/active", new { isActive = false });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AdminUserSetActive_Customer_ReturnsForbiddenAndDoesNotChangeUser()
    {
        await AuthenticateAsync("customer@forgemart.test", "Customer123!");
        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{MarkoId}/active", new { isActive = false });
        var isActive = await ReadDbAsync(db => db.Users.Single(user => user.Id == MarkoId).IsActive);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(isActive, Is.True);
        });
    }

    private async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private async Task AuthenticateAsync(string email, string password)
    {
        var auth = await LoginAsync(email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    private Task AuthenticateAdminAsync() => AuthenticateAsync("admin@forgemart.test", "Admin123!");

    private async Task SetCategoryActiveAsync(Guid id, bool isActive)
    {
        await WithDbAsync(async db =>
        {
            db.Categories.Single(category => category.Id == id).IsActive = isActive;
            await db.SaveChangesAsync();
        });
    }

    private async Task<T> ReadDbAsync<T>(Func<ForgeMartDbContext, T> query)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return query(scope.ServiceProvider.GetRequiredService<ForgeMartDbContext>());
    }

    private async Task WithDbAsync(Func<ForgeMartDbContext, Task> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ForgeMartDbContext>());
    }
}

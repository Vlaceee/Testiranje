using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ForgeMart.PlaywrightTests.Api;

[TestFixture]
[Category("Api")]
[NonParallelizable]
public sealed class ForgeMartApiPlaywrightTests
{
    private IPlaywright playwright = null!;
    private IAPIRequestContext request = null!;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        playwright = await Playwright.CreateAsync();
        request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Environment.GetEnvironmentVariable("FORGEMART_API_URL") ?? "http://localhost:5073",
            ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept"] = "application/json" }
        });
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        await request.DisposeAsync();
        playwright.Dispose();
    }

    [Test]
    public async Task Health_ReturnsHealthyApplicationAndDatabase()
    {
        var response = await request.GetAsync("/health");
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(response.TextAsync().Result, Does.Contain("Healthy"));
        });
    }

    [Test]
    public async Task Products_ReturnsFirstPage()
    {
        var response = await request.GetAsync("/api/v1/products?page=1&pageSize=12");
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.Ok, Is.True);
            Assert.That(json.RootElement.GetProperty("items").GetArrayLength(), Is.GreaterThan(0));
            Assert.That(json.RootElement.GetProperty("page").GetInt32(), Is.EqualTo(1));
        });
    }

    [TestCase(0, 20)]
    [TestCase(1, 0)]
    [TestCase(1, 101)]
    public async Task Products_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        var response = await request.GetAsync($"/api/v1/products?page={page}&pageSize={pageSize}");
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Products_Search_IsCaseInsensitiveAndPartial()
    {
        var response = await request.GetAsync("/api/v1/products?search=vol&page=1&pageSize=20");
        var body = await response.TextAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.Ok, Is.True);
            Assert.That(body, Does.Contain("VoltCraft").IgnoreCase);
        });
    }

    [Test]
    public async Task Categories_ReturnsAllSeededHardwareGroups()
    {
        var response = await request.GetAsync("/api/v1/categories");
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.Ok, Is.True);
            Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(8));
        });
    }

    [Test]
    public async Task Login_CustomerCredentials_ReturnCustomerJwt()
    {
        var response = await LoginAsync("customer@forgemart.test", "Customer123!");
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.Ok, Is.True);
            Assert.That(json.RootElement.GetProperty("accessToken").GetString(), Is.Not.Empty);
            Assert.That(json.RootElement.GetProperty("user").GetProperty("role").GetString(), Is.EqualTo("Customer"));
        });
    }

    [Test]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var response = await LoginAsync("customer@forgemart.test", "NotThePassword1!");
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var response = await request.PostAsync("/api/v1/auth/register", new()
        {
            DataObject = new { email = "customer@forgemart.test", password = "Customer123!", firstName = "Duplicate", lastName = "User" }
        });
        Assert.That(response.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task AdminInventory_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await request.GetAsync("/api/v1/admin/inventory");
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task AdminInventory_CustomerRequest_ReturnsForbidden()
    {
        var token = await TokenAsync("customer@forgemart.test", "Customer123!");
        var response = await request.GetAsync("/api/v1/admin/inventory", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }
        });
        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task AdminUsers_AdminRequest_ReturnsSeededUsers()
    {
        var token = await TokenAsync("admin@forgemart.test", "Admin123!");
        var response = await request.GetAsync("/api/v1/admin/users", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }
        });
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.Ok, Is.True);
            Assert.That(json.RootElement.GetArrayLength(), Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public async Task CurrentUser_ValidToken_ReturnsAuthenticatedProfile()
    {
        var token = await TokenAsync("customer@forgemart.test", "Customer123!");
        var response = await request.GetAsync("/api/v1/auth/me", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }
        });
        var body = await response.TextAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.Ok, Is.True);
            Assert.That(body, Does.Contain("customer@forgemart.test"));
        });
    }

    [Test]
    public async Task Product_UnknownIdentifier_ReturnsNotFound()
    {
        var response = await request.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        Assert.That(response.Status, Is.EqualTo(404));
    }

    
    private const string AdminEmail = "admin@forgemart.test";
    private const string AdminPassword = "Admin123!";
    private const string CustomerEmail = "customer@forgemart.test";
    private const string CustomerPassword = "Customer123!";
    private const string FirstProductId = "40000000-0000-0000-0000-000000000001";
    private const string FirstCategoryId = "20000000-0000-0000-0000-000000000001";

    [Test]
    public async Task SystemInfo_ReturnsDisplayNameAndApiVersion()
    {
        var response = await request.GetAsync("/api/v1/system/info");
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("displayName").GetString(), Is.EqualTo("Tools & Building Supplies"));
            Assert.That(json.RootElement.GetProperty("apiVersion").GetString(), Is.EqualTo("v1"));
        });
    }

    [Test]
    public async Task Register_UniqueCustomer_ReturnsCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var response = await request.PostAsync("/api/v1/auth/register", new()
        {
            DataObject = Registration($"api.new.{suffix}@forgemart.test")
        });
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(201));
            Assert.That(json.RootElement.GetProperty("accessToken").GetString(), Is.Not.Empty);
            Assert.That(json.RootElement.GetProperty("user").GetProperty("role").GetString(), Is.EqualTo("Customer"));
        });
    }

    [Test]
    public async Task Register_EmailWithUppercase_IsNormalizedToLowercase()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"API.NORMALIZED.{suffix}@FORGEMART.TEST";
        var response = await request.PostAsync("/api/v1/auth/register", new()
        {
            DataObject = Registration(email)
        });
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(201));
            Assert.That(json.RootElement.GetProperty("user").GetProperty("email").GetString(), Is.EqualTo(email.ToLowerInvariant()));
        });
    }

    [Test]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var response = await request.PostAsync("/api/v1/auth/register", new()
        {
            DataObject = Registration("not-an-email")
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Register_OneCharacterFirstName_ReturnsBadRequest()
    {
        var response = await request.PostAsync("/api/v1/auth/register", new()
        {
            DataObject = new
            {
                email = $"short-name.{Guid.NewGuid():N}@forgemart.test",
                password = "Strong123!",
                firstName = "A",
                lastName = "Tester"
            }
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Register_PasswordWithoutSymbol_ReturnsBadRequest()
    {
        var response = await request.PostAsync("/api/v1/auth/register", new()
        {
            DataObject = new
            {
                email = $"weak.{Guid.NewGuid():N}@forgemart.test",
                password = "Strong123",
                firstName = "Api",
                lastName = "Tester"
            }
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Login_Admin_ReturnsAdminRole()
    {
        var response = await LoginAsync(AdminEmail, AdminPassword);
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("user").GetProperty("role").GetString(), Is.EqualTo("Admin"));
        });
    }

    [Test]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await LoginAsync($"missing.{Guid.NewGuid():N}@forgemart.test", CustomerPassword);
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task CurrentUser_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await request.GetAsync("/api/v1/auth/me");
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task CurrentUser_CustomerToken_ReturnsActiveCustomer()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.GetAsync("/api/v1/auth/me", Authorized(token));
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("email").GetString(), Is.EqualTo(CustomerEmail));
            Assert.That(json.RootElement.GetProperty("isActive").GetBoolean(), Is.True);
        });
    }

    [Test]
    public async Task Products_PageSizeThree_ReturnsThreeItems()
    {
        var response = await request.GetAsync("/api/v1/products?page=1&pageSize=3");
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("items").GetArrayLength(), Is.EqualTo(3));
            Assert.That(json.RootElement.GetProperty("pageSize").GetInt32(), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Products_NameDescending_ReturnsExpectedFirstProduct()
    {
        var response = await request.GetAsync("/api/v1/products?page=1&pageSize=20&sort=NameDesc");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items.GetArrayLength(), Is.GreaterThan(0));
            Assert.That(items[0].GetProperty("name").GetString(), Is.EqualTo("Work Gloves"));
        });
    }

    [Test]
    public async Task Products_BrandFilter_ReturnsOnlyRequestedBrand()
    {
        var response = await request.GetAsync("/api/v1/products?brand=VoltCraft&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("brand").GetString() == "VoltCraft"), Is.True);
        });
    }

    [Test]
    public async Task Products_CategoryFilter_ReturnsOnlyRequestedCategory()
    {
        var response = await request.GetAsync($"/api/v1/products?categoryId={FirstCategoryId}&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("categoryId").GetString() == FirstCategoryId), Is.True);
        });
    }

    [Test]
    public async Task Products_InStockTrue_ReturnsOnlyAvailableProducts()
    {
        var response = await request.GetAsync("/api/v1/products?inStock=true&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("stockQuantity").GetDecimal() > 0), Is.True);
        });
    }

    [Test]
    public async Task Products_InStockFalse_ReturnsOnlyUnavailableProducts()
    {
        var response = await request.GetAsync("/api/v1/products?inStock=false&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("stockQuantity").GetDecimal() <= 0), Is.True);
        });
    }

    [Test]
    public async Task Products_DiscountedTrue_ReturnsOnlyDiscountedProducts()
    {
        var response = await request.GetAsync("/api/v1/products?discounted=true&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("hasActiveDiscount").GetBoolean()), Is.True);
        });
    }

    [Test]
    public async Task Products_DiscountedFalse_ReturnsOnlyNonDiscountedProducts()
    {
        var response = await request.GetAsync("/api/v1/products?discounted=false&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => !item.GetProperty("hasActiveDiscount").GetBoolean()), Is.True);
        });
    }

    [Test]
    public async Task Products_MinimumPrice_ReturnsOnlyProductsAtOrAboveMinimum()
    {
        var response = await request.GetAsync("/api/v1/products?minPrice=5000&page=1&pageSize=100");
        using var json = await JsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("price").GetDecimal() >= 5000m), Is.True);
        });
    }

    [Test]
    public async Task Products_MinimumGreaterThanMaximum_ReturnsBadRequest()
    {
        var response = await request.GetAsync("/api/v1/products?minPrice=1000&maxPrice=500");
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Products_UnknownSortValue_ReturnsBadRequest()
    {
        var response = await request.GetAsync("/api/v1/products?sort=UnknownSortValue");
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Product_ExistingIdentifier_ReturnsDetailedProduct()
    {
        var response = await request.GetAsync($"/api/v1/products/{FirstProductId}");
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("id").GetString(), Is.EqualTo(FirstProductId));
            Assert.That(json.RootElement.GetProperty("description").GetString(), Is.Not.Empty);
        });
    }

    [Test]
    public async Task Product_MalformedIdentifier_ReturnsNotFound()
    {
        var response = await request.GetAsync("/api/v1/products/not-a-guid");
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task ProductCreate_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await request.PostAsync("/api/v1/products", new() { DataObject = new { } });
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task ProductCreate_CustomerRequest_ReturnsForbidden()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.PostAsync("/api/v1/products", new()
        {
            Headers = Bearer(token),
            DataObject = new { }
        });
        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task ProductCreate_InvalidShortName_ReturnsBadRequest()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.PostAsync("/api/v1/products", new()
        {
            Headers = Bearer(token),
            DataObject = ProductPayload("A")
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task ProductUpdate_UnknownIdentifier_ReturnsNotFound()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.PutAsync($"/api/v1/products/{Guid.NewGuid()}", new()
        {
            Headers = Bearer(token),
            DataObject = ProductPayload("Valid API Product")
        });
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task ProductDeactivate_UnknownIdentifier_ReturnsNotFound()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}", Authorized(token));
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task ProductReactivate_AlreadyActiveProduct_ReturnsConflict()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.PostAsync($"/api/v1/products/{FirstProductId}/reactivate", new()
        {
            Headers = Bearer(token)
        });
        Assert.That(response.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Categories_PublicListContainsOnlyActiveCategories()
    {
        var response = await request.GetAsync("/api/v1/categories?includeInactive=true");
        using var json = await JsonAsync(response);
        var items = json.RootElement.EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.All(item => item.GetProperty("isActive").GetBoolean()), Is.True);
        });
    }

    [Test]
    public async Task Categories_PublicListContainsKnownPowerToolsCategory()
    {
        var response = await request.GetAsync("/api/v1/categories");
        using var json = await JsonAsync(response);
        var names = json.RootElement.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(names, Does.Contain("Power Tools"));
        });
    }

    [Test]
    public async Task CategoryCreate_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await request.PostAsync("/api/v1/categories", new()
        {
            DataObject = CategoryPayload("Anonymous category")
        });
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task CategoryCreate_CustomerRequest_ReturnsForbidden()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.PostAsync("/api/v1/categories", new()
        {
            Headers = Bearer(token),
            DataObject = CategoryPayload("Customer category")
        });
        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task CategoryCreate_ShortDescription_ReturnsBadRequest()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.PostAsync("/api/v1/categories", new()
        {
            Headers = Bearer(token),
            DataObject = new { name = $"Category {Guid.NewGuid():N}", description = "short" }
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task CategoryUpdate_UnknownIdentifier_ReturnsNotFound()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.PutAsync($"/api/v1/categories/{Guid.NewGuid()}", new()
        {
            Headers = Bearer(token),
            DataObject = CategoryPayload("Unknown category")
        });
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task CategoryDeactivate_UnknownIdentifier_ReturnsNotFound()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}", Authorized(token));
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task CategoryReactivate_AlreadyActiveCategory_ReturnsConflict()
    {
        var token = await TokenAsync(AdminEmail, AdminPassword);
        var response = await request.PostAsync($"/api/v1/categories/{FirstCategoryId}/reactivate", new()
        {
            Headers = Bearer(token)
        });
        Assert.That(response.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Cart_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await request.GetAsync("/api/v1/cart");
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task Cart_CustomerRequest_ReturnsCartContract()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.GetAsync("/api/v1/cart", Authorized(token));
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.TryGetProperty("items", out _), Is.True);
            Assert.That(json.RootElement.TryGetProperty("grandTotal", out _), Is.True);
        });
    }

    [Test]
    public async Task CartSet_ZeroQuantity_ReturnsBadRequest()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.PutAsync($"/api/v1/cart/items/{FirstProductId}", new()
        {
            Headers = Bearer(token),
            DataObject = new { quantity = 0 }
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task CartSet_UnknownProduct_ReturnsNotFound()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.PutAsync($"/api/v1/cart/items/{Guid.NewGuid()}", new()
        {
            Headers = Bearer(token),
            DataObject = new { quantity = 1 }
        });
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task CartRemove_ProductNotInCart_ReturnsNotFound()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.DeleteAsync($"/api/v1/cart/items/{Guid.NewGuid()}", Authorized(token));
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task CartMerge_EmptyItems_ReturnsBadRequest()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.PostAsync("/api/v1/cart/merge", new()
        {
            Headers = Bearer(token),
            DataObject = new { items = Array.Empty<object>() }
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task CartMerge_DuplicateProductIdentifiers_ReturnsBadRequest()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.PostAsync("/api/v1/cart/merge", new()
        {
            Headers = Bearer(token),
            DataObject = new
            {
                items = new[]
                {
                    new { productId = FirstProductId, quantity = 1 },
                    new { productId = FirstProductId, quantity = 1 }
                }
            }
        });
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Orders_AnonymousRequest_ReturnsUnauthorized()
    {
        var response = await request.GetAsync("/api/v1/orders");
        Assert.That(response.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task Orders_CustomerRequest_ReturnsPagedContract()
    {
        var token = await TokenAsync(CustomerEmail, CustomerPassword);
        var response = await request.GetAsync("/api/v1/orders?page=1&pageSize=20", Authorized(token));
        using var json = await JsonAsync(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("page").GetInt32(), Is.EqualTo(1));
            Assert.That(json.RootElement.TryGetProperty("items", out _), Is.True);
        });
    }

    private Task<IAPIResponse> LoginAsync(string email, string password) =>
        request.PostAsync("/api/v1/auth/login", new() { DataObject = new { email, password } });

    private async Task<string> TokenAsync(string email, string password)
    {
        var response = await LoginAsync(email, password);
        Assert.That(response.Status, Is.EqualTo(200), $"Login failed for {email}: {await response.TextAsync()}");
        using var json = await JsonAsync(response);
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static APIRequestContextOptions Authorized(string token) => new() { Headers = Bearer(token) };

    private static Dictionary<string, string> Bearer(string token) =>
        new() { ["Authorization"] = $"Bearer {token}" };

    private static object Registration(string email) => new
    {
        email,
        password = "Strong123!",
        firstName = "Api",
        lastName = "Tester"
    };

    private static object CategoryPayload(string name) => new
    {
        name,
        description = "A deterministic category created for Playwright API testing."
    };

    private static object ProductPayload(string name) => new
    {
        name,
        sku = $"API-{Guid.NewGuid():N}"[..30],
        description = "A deterministic product payload used by Playwright API tests.",
        brand = "TestForge",
        categoryId = FirstCategoryId,
        price = 1000m,
        costPrice = 600m,
        vatRate = 20m,
        discountPercentage = 0m,
        unitOfMeasure = "Piece",
        unitsPerPackage = 1,
        imageUrl = "/assets/products/tool-placeholder.svg",
        initialStockQuantity = 5m,
        minimumStockLevel = 2m
    };

    private static async Task<JsonDocument> JsonAsync(IAPIResponse response) =>
        JsonDocument.Parse(await response.TextAsync());
}

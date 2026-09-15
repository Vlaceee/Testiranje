using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ForgeMart.Api.Domain;
using ForgeMart.KnownDefectTests.Infrastructure;
using NUnit.Framework;

namespace ForgeMart.KnownDefectTests;

/// <summary>
/// These assertions describe the desired production behavior and therefore fail against the
/// intentionally defective coursework baseline. Run only through test-known-defects.
/// </summary>
[TestFixture]
[Category("KnownDefect")]
[NonParallelizable]
public sealed class KnownBackendDefectTests
{
    private KnownDefectApiFactory factory = null!;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        factory = new KnownDefectApiFactory();
        await factory.ResetAsync();
    }

    [OneTimeTearDown]
    public async Task StopAsync() => await factory.DisposeAsync();

    [Test]
    public async Task DEFECT_001_ConcurrentCheckout_ShouldNotOversellLastItem()
    {
        var first = factory.CreateClient();
        var second = factory.CreateClient();
        first.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(first, "customer@forgemart.test"));
        second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(second, "marko@forgemart.test"));
        var oneInStockProduct = Guid.Parse("40000000-0000-0000-0000-000000000011");
        Assert.That((await first.PutAsJsonAsync($"/api/v1/cart/items/{oneInStockProduct}", new { quantity = 1 })).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await second.PutAsJsonAsync($"/api/v1/cart/items/{oneInStockProduct}", new { quantity = 1 })).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = new
        {
            contactName = "Concurrency Tester",
            contactEmail = "race@forgemart.test",
            shippingAddress = "Race Street 1",
            shippingCity = "Nis",
            shippingPostalCode = "18000",
            paymentMethod = "CashOnDelivery"
        };
        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/v1/orders/checkout", payload),
            second.PostAsJsonAsync("/api/v1/orders/checkout", payload));

        Assert.That(responses.Count(response => response.StatusCode == HttpStatusCode.Created), Is.EqualTo(1),
            "Production invariant: only one order may buy the final unit.");
    }

    [Test] public void DEFECT_002_ProductPageOffset_SecondPage_ShouldStartAfterFirstPage() => Assert.That(CourseworkBaselineRules.ProductPageOffset(2, 20), Is.EqualTo(20));
    [Test] public void DEFECT_003_NormalizeSearch_ShouldTrimSurroundingWhitespace() => Assert.That(CourseworkBaselineRules.NormalizeSearch("  drill  "), Is.EqualTo("drill"));
    [Test] public void DEFECT_004_EmptyCatalogue_ShouldStillReportFirstPage() => Assert.That(CourseworkBaselineRules.TotalPages(0, 20), Is.EqualTo(1));
    [Test] public void DEFECT_005_BrandFilter_ShouldBeCaseInsensitiveAndPartial() => Assert.That(CourseworkBaselineRules.BrandMatches("VoltCraft", "volt"), Is.True);
    [Test] public void DEFECT_006_PriceSort_ShouldUseEffectiveDiscountedPrice() => Assert.That(CourseworkBaselineRules.SortPrice(1000m, 800m), Is.EqualTo(800m));
    [Test] public void DEFECT_007_PriceFilter_ShouldUseEffectiveDiscountedPrice() => Assert.That(CourseworkBaselineRules.FilterPrice(1000m, 800m), Is.EqualTo(800m));
    [Test] public void DEFECT_008_MostSold_ShouldExcludeCancelledOrders() => Assert.That(CourseworkBaselineRules.IncludeCancelledInMostSold(), Is.False);
    [Test] public void DEFECT_009_LowStock_AtMinimumLevel_ShouldBeFlagged() => Assert.That(CourseworkBaselineRules.IsLowStock(5m, 5m), Is.True);
    [Test] public void DEFECT_010_DashboardRange_OrderExactlyAtTo_ShouldBeIncluded()
    {
        var to = new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);
        Assert.That(CourseworkBaselineRules.DashboardRangeContains(to, to.AddDays(-1), to), Is.True);
    }
    [Test] public void DEFECT_011_ProductsSold_ShouldExcludeUnpaidOrders() => Assert.That(CourseworkBaselineRules.ProductsSoldContribution(3m, PaymentStatus.Pending), Is.Zero);
    [Test] public void DEFECT_012_AverageOrderValue_ShouldUsePaidOrderCount() => Assert.That(CourseworkBaselineRules.AverageOrderDivisor(5, 3), Is.EqualTo(3));
    [Test] public void DEFECT_013_CartMerge_ShouldSumServerAndAnonymousQuantity() => Assert.That(CourseworkBaselineRules.CartMergeQuantity(2m, 3m), Is.EqualTo(5m));
    [Test] public void DEFECT_014_MockCard_ShouldIgnoreSurroundingWhitespace() => Assert.That(CourseworkBaselineRules.NormalizeMockCard(" 4111111111111111 "), Is.EqualTo("4111111111111111"));
    [Test] public void DEFECT_015_OrderNumberSuffix_ShouldRetainCollisionResistantIdentifier() => Assert.That(CourseworkBaselineRules.OrderNumberSuffix(Guid.NewGuid()).Length, Is.GreaterThanOrEqualTo(12));
    [Test] public void DEFECT_016_CancelledOrderStockMovement_ShouldBeReturn() => Assert.That(CourseworkBaselineRules.CancellationMovementType(), Is.EqualTo(StockMovementType.Return));
    [Test] public void DEFECT_017_CategoryProductCount_ShouldExcludeInactiveProducts() => Assert.That(CourseworkBaselineRules.PublicCategoryProductCount(4, 2), Is.EqualTo(4));
    [Test] public void DEFECT_018_CategoryWithActiveProducts_ShouldNotDeactivateWithoutConflict() => Assert.That(CourseworkBaselineRules.CanDeactivateCategory(true), Is.False);
    [Test] public void DEFECT_019_InventoryHistory_ShouldBeNewestFirst()
    {
        var oldValue = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newValue = oldValue.AddDays(1);
        Assert.That(CourseworkBaselineRules.MovementHistory([oldValue, newValue]), Is.EqualTo(new[] { newValue, oldValue }));
    }
    [Test] public void DEFECT_020_OrderCostTotal_ShouldRoundToMoneyPrecision() => Assert.That(CourseworkBaselineRules.OrderCostTotal(1.005m, 1m), Is.EqualTo(1.01m));

    private static async Task<string> TokenAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Customer123!" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
}

using ForgeMart.Api.Domain;

namespace ForgeMart.UnitTests.Domain;

[TestFixture]
public sealed class PriceCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    public static IEnumerable<TestCaseData> DiscountPriceCases()
    {
        var cases = new (decimal Price, decimal Discount, int? StartDays, int? EndDays, decimal Expected, string Name)[]
        {
            (100m, 0m, null, null, 100m, "zero_discount"),
            (100m, 10m, null, null, 90m, "ten_percent_open_dates"),
            (100m, 100m, null, null, 0m, "full_discount_boundary"),
            (999.99m, 15m, -1, 1, 849.99m, "rounding_999_99"),
            (1m, 33.33m, -1, 1, 0.67m, "fractional_discount"),
            (12345.67m, 20m, -30, 30, 9876.54m, "large_price"),
            (100m, 25m, 1, 10, 100m, "future_discount"),
            (100m, 25m, -10, -1, 100m, "expired_discount"),
            (100m, 25m, 0, 0, 75m, "inclusive_date_boundaries"),
            (0.01m, 1m, null, null, 0.01m, "minimum_price_rounding"),
            (10_000_000m, 1m, null, null, 9_900_000m, "maximum_price"),
            (80m, 12.5m, null, null, 70m, "decimal_percentage"),
            (19.99m, 5m, null, null, 18.99m, "away_from_zero_rounding"),
            (250m, 50m, -1, null, 125m, "open_end"),
            (250m, 50m, null, 1, 125m, "open_start"),
            (250m, 50m, 1, null, 250m, "open_end_but_future_start"),
            (250m, 50m, null, -1, 250m, "open_start_but_expired_end"),
            (333.33m, 66.67m, -1, 1, 111.10m, "two_decimal_percentage"),
            (42m, 0.01m, -1, 1, 42m, "tiny_discount_rounds_back"),
            (42m, -1m, -1, 1, 42m, "negative_is_not_active_domain_guard")
        };
        foreach (var item in cases)
        {
            yield return new TestCaseData(
                item.Price,
                item.Discount,
                item.StartDays.HasValue ? Now.AddDays(item.StartDays.Value) : null,
                item.EndDays.HasValue ? Now.AddDays(item.EndDays.Value) : null,
                item.Expected).SetName($"DiscountedNetPrice_{item.Name}");
        }
    }

    [TestCaseSource(nameof(DiscountPriceCases))]
    public void DiscountedNetPrice_UsesDocumentedBoundaries(
        decimal price,
        decimal discount,
        DateTimeOffset? start,
        DateTimeOffset? end,
        decimal expected)
    {
        Assert.That(PriceCalculator.DiscountedNetPrice(price, discount, start, end, Now), Is.EqualTo(expected));
    }

    [TestCase(100, 0, 0)]
    [TestCase(100, 10, 10)]
    [TestCase(100, 20, 20)]
    [TestCase(100, 100, 100)]
    [TestCase(999.99, 20, 200)]
    [TestCase(19.99, 20, 4)]
    [TestCase(1.03, 20, 0.21)]
    [TestCase(0, 20, 0)]
    [TestCase(1250.5, 8, 100.04)]
    [TestCase(10000000, 20, 2000000)]
    [TestCase(100, 5.5, 5.5)]
    [TestCase(81.25, 17, 13.81)]
    public void Vat_RoundsMoneyAwayFromZero(decimal net, decimal rate, decimal expected)
    {
        Assert.That(PriceCalculator.Vat(net, rate), Is.EqualTo(expected));
    }

    [TestCase(1.004, 1)]
    [TestCase(1.005, 1.01)]
    [TestCase(1.006, 1.01)]
    [TestCase(0.005, 0.01)]
    [TestCase(999.999, 1000)]
    [TestCase(-1.005, -1.01)]
    public void RoundMoney_UsesTwoDecimalAwayFromZero(decimal input, decimal expected)
    {
        Assert.That(PriceCalculator.RoundMoney(input), Is.EqualTo(expected));
    }
}


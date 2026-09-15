using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Domain;

namespace ForgeMart.UnitTests.Validation;

[TestFixture]
public sealed class ProductRequestValidationTests
{
    [TestCase("A", false)]
    [TestCase("AB", true)]
    [TestCase("Workshop Drill", true)]
    [TestCaseSource(nameof(NameBoundaryCases))]
    public void Name_UsesTwoToOneHundredFiftyBoundary(string name, bool valid)
    {
        Assert.That(IsValid(Create(name: name)), Is.EqualTo(valid));
    }

    private static IEnumerable<TestCaseData> NameBoundaryCases()
    {
        yield return new TestCaseData(new string('N', 150), true).SetName("Name_150_valid");
        yield return new TestCaseData(new string('N', 151), false).SetName("Name_151_invalid");
    }

    [TestCase("AB", false)]
    [TestCase("ABC", true)]
    [TestCase("FM-01-001", true)]
    [TestCase("sku_lowercase", false)]
    [TestCase("SKU WITH SPACE", false)]
    [TestCase("SKU/SLASH", false)]
    [TestCaseSource(nameof(SkuLengthCases))]
    public void Sku_ValidatesLengthAndCharacterEquivalenceClasses(string sku, bool valid)
    {
        Assert.That(IsValid(Create(sku: sku)), Is.EqualTo(valid));
    }

    private static IEnumerable<TestCaseData> SkuLengthCases()
    {
        yield return new TestCaseData(new string('S', 30), true).SetName("Sku_30_valid");
        yield return new TestCaseData(new string('S', 31), false).SetName("Sku_31_invalid");
    }

    [TestCase(0, false)]
    [TestCase(0.01, true)]
    [TestCase(1, true)]
    [TestCase(9999999.99, true)]
    [TestCase(10000000, true)]
    [TestCase(10000000.01, false)]
    [TestCase(-1, false)]
    public void Price_UsesDocumentedMonetaryBoundaries(decimal price, bool valid)
    {
        Assert.That(IsValid(Create(price: price, costPrice: 0m)), Is.EqualTo(valid));
    }

    [TestCase(-0.01, 10, false)]
    [TestCase(0, 10, true)]
    [TestCase(9.99, 10, true)]
    [TestCase(10, 10, true)]
    [TestCase(10.01, 10, false)]
    [TestCase(100, 99, false)]
    public void CostPrice_MustBeNonNegativeAndNotExceedPrice(decimal cost, decimal price, bool valid)
    {
        Assert.That(IsValid(Create(price: price, costPrice: cost)), Is.EqualTo(valid));
    }

    [TestCase(-0.01, false)]
    [TestCase(0, true)]
    [TestCase(0.01, true)]
    [TestCase(50, true)]
    [TestCase(99.99, true)]
    [TestCase(100, true)]
    [TestCase(100.01, false)]
    public void Discount_UsesZeroToOneHundredBoundary(decimal discount, bool valid)
    {
        Assert.That(IsValid(Create(discount: discount)), Is.EqualTo(valid));
    }

    [TestCase(-0.01, false)]
    [TestCase(0, true)]
    [TestCase(10, true)]
    [TestCase(20, true)]
    [TestCase(100, true)]
    [TestCase(100.01, false)]
    public void Vat_UsesConfiguredRange(decimal vat, bool valid)
    {
        Assert.That(IsValid(Create(vat: vat)), Is.EqualTo(valid));
    }

    [TestCase(UnitOfMeasure.Pack, 1, true)]
    [TestCase(UnitOfMeasure.Pack, 100, true)]
    [TestCase(UnitOfMeasure.Pack, 100000, true)]
    [TestCase(UnitOfMeasure.Pack, 0, false)]
    [TestCase(UnitOfMeasure.Piece, 1, true)]
    [TestCase(UnitOfMeasure.Piece, 2, false)]
    [TestCase(UnitOfMeasure.Meter, 100, false)]
    public void UnitsPerPackage_IsSimpleAndConsistent(UnitOfMeasure unit, int unitsPerPackage, bool valid)
    {
        Assert.That(IsValid(Create(unit: unit, unitsPerPackage: unitsPerPackage)), Is.EqualTo(valid));
    }

    [Test]
    public void DiscountDates_AllowOpenRange() => Assert.That(IsValid(Create()), Is.True);

    [Test]
    public void DiscountDates_AllowEqualBoundary()
    {
        var date = DateTimeOffset.Parse("2026-08-21T12:00:00Z", CultureInfo.InvariantCulture);
        Assert.That(IsValid(Create(start: date, end: date)), Is.True);
    }

    [Test]
    public void DiscountDates_RejectStartAfterEnd()
    {
        Assert.That(IsValid(Create(
            start: DateTimeOffset.Parse("2026-08-22T00:00:00Z", CultureInfo.InvariantCulture),
            end: DateTimeOffset.Parse("2026-08-21T00:00:00Z", CultureInfo.InvariantCulture))), Is.False);
    }

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(100000000, true)]
    [TestCase(100000001, false)]
    public void Stock_UsesNonNegativeBoundary(decimal stock, bool valid)
    {
        Assert.That(IsValid(Create(stock: stock)), Is.EqualTo(valid));
    }

    private static bool IsValid(UpsertProductRequest request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), [], true);

    private static UpsertProductRequest Create(
        string name = "Valid Drill",
        string sku = "FM-01-001",
        decimal price = 100m,
        decimal costPrice = 50m,
        decimal discount = 0m,
        decimal vat = 20m,
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        int unitsPerPackage = 1,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        decimal stock = 10m) => new()
    {
        Name = name,
        Sku = sku,
        Description = "A valid product description.",
        Brand = "IronPeak",
        CategoryId = Guid.NewGuid(),
        Price = price,
        CostPrice = costPrice,
        VatRate = vat,
        DiscountPercentage = discount,
        DiscountStart = start,
        DiscountEnd = end,
        UnitOfMeasure = unit,
        UnitsPerPackage = unitsPerPackage,
        ImageUrl = "/assets/products/tool-placeholder.svg",
        InitialStockQuantity = stock,
        MinimumStockLevel = 1m
    };
}

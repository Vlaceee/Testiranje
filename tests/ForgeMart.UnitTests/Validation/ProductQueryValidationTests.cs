using System.ComponentModel.DataAnnotations;
using ForgeMart.Api.Contracts;

namespace ForgeMart.UnitTests.Validation;

[TestFixture]
public sealed class ProductQueryValidationTests
{
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(1000000, true)]
    public void Page_MustStartAtOne(int page, bool valid) => Assert.That(IsValid(new ProductQuery { Page = page }), Is.EqualTo(valid));

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(20, true)]
    [TestCase(99, true)]
    [TestCase(100, true)]
    [TestCase(101, false)]
    public void PageSize_UsesOneToOneHundredBoundary(int pageSize, bool valid) =>
        Assert.That(IsValid(new ProductQuery { PageSize = pageSize }), Is.EqualTo(valid));

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(0.01, true)]
    [TestCase(10000000, true)]
    [TestCase(10000000.01, false)]
    public void MinPrice_UsesCatalogBoundary(decimal price, bool valid) =>
        Assert.That(IsValid(new ProductQuery { MinPrice = price }), Is.EqualTo(valid));

    private static bool IsValid(ProductQuery query) =>
        Validator.TryValidateObject(query, new ValidationContext(query), [], true);
}

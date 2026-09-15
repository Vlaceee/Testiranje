using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;

namespace ForgeMart.UnitTests.Domain;

[TestFixture]
public sealed class CartRulesTests
{
    [TestCase(1, 1, UnitOfMeasure.Piece)]
    [TestCase(10, 10, UnitOfMeasure.Piece)]
    [TestCase(1, 100, UnitOfMeasure.Pack)]
    [TestCase(2, 2, UnitOfMeasure.Pack)]
    [TestCase(0.001, 1, UnitOfMeasure.Meter)]
    [TestCase(0.5, 1, UnitOfMeasure.Meter)]
    [TestCase(1.25, 2, UnitOfMeasure.Meter)]
    [TestCase(3.333, 10, UnitOfMeasure.Kilogram)]
    [TestCase(0.25, 1, UnitOfMeasure.SquareMeter)]
    [TestCase(99999, 100000, UnitOfMeasure.Piece)]
    [TestCase(100000, 100000, UnitOfMeasure.Pack)]
    [TestCase(7, 7.5, UnitOfMeasure.Piece)]
    public void ValidateQuantity_AcceptsValidEquivalenceClasses(decimal requested, decimal stock, UnitOfMeasure unit)
    {
        Assert.DoesNotThrow(() => CartRules.ValidateQuantity(requested, stock, unit));
    }

    [TestCase(0, 1, UnitOfMeasure.Piece, 400)]
    [TestCase(-1, 1, UnitOfMeasure.Piece, 400)]
    [TestCase(-0.001, 1, UnitOfMeasure.Meter, 400)]
    [TestCase(2, 1, UnitOfMeasure.Piece, 409)]
    [TestCase(1.001, 1, UnitOfMeasure.Meter, 409)]
    [TestCase(100001, 100000, UnitOfMeasure.Pack, 409)]
    [TestCase(1.5, 2, UnitOfMeasure.Piece, 400)]
    [TestCase(1.01, 2, UnitOfMeasure.Pack, 400)]
    [TestCase(0.5, 2, UnitOfMeasure.Pack, 400)]
    [TestCase(2.999, 3, UnitOfMeasure.Piece, 400)]
    public void ValidateQuantity_RejectsInvalidBoundaries(decimal requested, decimal stock, UnitOfMeasure unit, int status)
    {
        var exception = Assert.Throws<ApiException>(() => CartRules.ValidateQuantity(requested, stock, unit));
        Assert.That(exception!.StatusCode, Is.EqualTo(status));
    }
}


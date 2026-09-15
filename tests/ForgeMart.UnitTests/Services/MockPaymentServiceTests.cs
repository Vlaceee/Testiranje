using ForgeMart.Api.Contracts;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using ForgeMart.Api.Services;

namespace ForgeMart.UnitTests.Services;

[TestFixture]
public sealed class MockPaymentServiceTests
{
    private readonly MockPaymentService service = new();

    [Test]
    public void CashOnDelivery_RemainsUnpaid() => Assert.That(service.IsPaid(Request(PaymentMethod.CashOnDelivery, null)), Is.False);

    [Test]
    public void SuccessTestCard_IsPaid() => Assert.That(service.IsPaid(Request(PaymentMethod.MockCard, "4111111111111111")), Is.True);

    [TestCase("4000000000000002", "declined")]
    [TestCase("", "documented")]
    [TestCase(null, "documented")]
    [TestCase("4111111111111112", "documented")]
    [TestCase("real-card-must-never-be-used", "documented")]
    public void InvalidOrDeclinedCards_AreRejected(string? card, string messageFragment)
    {
        var exception = Assert.Throws<ApiException>(() => service.IsPaid(Request(PaymentMethod.MockCard, card)));
        Assert.That(exception!.Message, Does.Contain(messageFragment).IgnoreCase);
    }

    private static CheckoutRequest Request(PaymentMethod method, string? card) => new()
    {
        ContactName = "Test Customer",
        ContactEmail = "customer@example.test",
        ShippingAddress = "Test Street 1",
        ShippingCity = "Nis",
        ShippingPostalCode = "18000",
        PaymentMethod = method,
        MockCardNumber = card
    };
}

using ForgeMart.Api.Contracts;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;

namespace ForgeMart.Api.Services;

public sealed class MockPaymentService
{
    public bool IsPaid(CheckoutRequest request)
    {
        if (request.PaymentMethod == PaymentMethod.CashOnDelivery)
        {
            return false;
        }

        var cardNumber = CourseworkBaselineRules.NormalizeMockCard(request.MockCardNumber);
        if (cardNumber == "4000000000000002")
        {
            throw ApiException.BadRequest("Simulated card payment was declined.");
        }

        if (cardNumber != "4111111111111111")
        {
            throw ApiException.BadRequest("Use a documented ForgeMart fake card number for simulated payment.");
        }

        return true;
    }
}

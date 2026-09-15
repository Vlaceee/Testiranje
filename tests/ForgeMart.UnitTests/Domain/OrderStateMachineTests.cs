using ForgeMart.Api.Domain;

namespace ForgeMart.UnitTests.Domain;

[TestFixture]
public sealed class OrderStateMachineTests
{
    public static IEnumerable<TestCaseData> EveryAdminTransition()
    {
        foreach (var current in Enum.GetValues<OrderStatus>())
        {
            foreach (var target in Enum.GetValues<OrderStatus>())
            {
                var expected = (current, target) switch
                {
                    (OrderStatus.PendingPayment, OrderStatus.Paid) => true,
                    (OrderStatus.PendingPayment, OrderStatus.Cancelled) => true,
                    (OrderStatus.Paid, OrderStatus.Processing) => true,
                    (OrderStatus.Processing, OrderStatus.Shipped) => true,
                    (OrderStatus.Shipped, OrderStatus.Delivered) => true,
                    _ => false
                };
                yield return new TestCaseData(current, target, expected)
                    .SetName($"AdminTransition_{current}_to_{target}_{(expected ? "allowed" : "rejected")}");
            }
        }
    }

    [TestCaseSource(nameof(EveryAdminTransition))]
    public void CanAdminTransition_ImplementsCompleteStateMatrix(OrderStatus current, OrderStatus target, bool expected)
    {
        Assert.That(OrderStateMachine.CanAdminTransition(current, target), Is.EqualTo(expected));
    }

    public static IEnumerable<TestCaseData> EveryCustomerCancellationDecision()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            foreach (var payment in Enum.GetValues<PaymentStatus>())
            {
                var expected = status == OrderStatus.PendingPayment && payment != PaymentStatus.Paid;
                yield return new TestCaseData(status, payment, expected)
                    .SetName($"CustomerCancel_{status}_{payment}_{(expected ? "allowed" : "rejected")}");
            }
        }
    }

    [TestCaseSource(nameof(EveryCustomerCancellationDecision))]
    public void CanCustomerCancel_RejectsEveryPaidOrder(OrderStatus status, PaymentStatus payment, bool expected)
    {
        Assert.That(OrderStateMachine.CanCustomerCancel(status, payment), Is.EqualTo(expected));
    }
}


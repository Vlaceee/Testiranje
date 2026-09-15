namespace ForgeMart.Api.Domain;

public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AdminTransitions = new()
    {
        [OrderStatus.PendingPayment] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Processing],
        [OrderStatus.Processing] = [OrderStatus.Shipped],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static bool CanAdminTransition(OrderStatus current, OrderStatus target) =>
        AdminTransitions[current].Contains(target);

    public static bool CanCustomerCancel(OrderStatus status, PaymentStatus paymentStatus) =>
        status == OrderStatus.PendingPayment && paymentStatus != PaymentStatus.Paid;
}


namespace ForgeMart.Api.Domain;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string OrderNumber { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal CostTotal { get; set; }
    public required string ContactName { get; set; }
    public required string ContactEmail { get; set; }
    public required string ShippingAddress { get; set; }
    public required string ShippingCity { get; set; }
    public required string ShippingPostalCode { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string ProductNameSnapshot { get; set; }
    public required string SkuSnapshot { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal VatRate { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineVat { get; set; }
    public decimal LineTotal { get; set; }
}


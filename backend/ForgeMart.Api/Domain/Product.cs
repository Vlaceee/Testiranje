namespace ForgeMart.Api.Domain;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Sku { get; set; }
    public required string Description { get; set; }
    public required string Brand { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20m;
    public decimal DiscountPercentage { get; set; }
    public DateTimeOffset? DiscountStart { get; set; }
    public DateTimeOffset? DiscountEnd { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public int UnitsPerPackage { get; set; } = 1;
    public required string ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Inventory Inventory { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public bool HasActiveDiscount(DateTimeOffset now) =>
        PriceCalculator.IsDiscountActive(DiscountPercentage, DiscountStart, DiscountEnd, now);

    public decimal CurrentNetPrice(DateTimeOffset now)
    {
        return PriceCalculator.DiscountedNetPrice(Price, DiscountPercentage, DiscountStart, DiscountEnd, now);
    }
}

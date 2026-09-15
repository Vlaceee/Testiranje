namespace ForgeMart.Api.Domain;

public sealed class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;
    public decimal QuantityChange { get; set; }
    public StockMovementType Type { get; set; }
    public required string Reason { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}


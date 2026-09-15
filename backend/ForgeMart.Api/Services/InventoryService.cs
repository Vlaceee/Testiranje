using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class InventoryService(ForgeMartDbContext db, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<InventoryResponse>> ListAsync(bool lowStockOnly, CancellationToken cancellationToken) =>
        await db.Inventories.AsNoTracking()
            .Include(inventory => inventory.Product)
            .Where(inventory => !lowStockOnly || inventory.Quantity <= inventory.MinimumStockLevel)
            .OrderBy(inventory => inventory.Quantity - inventory.MinimumStockLevel)
            .Select(inventory => ToResponse(inventory))
            .ToListAsync(cancellationToken);

    public async Task<InventoryResponse> AdjustAsync(
        Guid productId,
        InventoryAdjustmentRequest request,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var inventory = await db.Inventories.Include(candidate => candidate.Product)
            .SingleOrDefaultAsync(candidate => candidate.ProductId == productId, cancellationToken)
            ?? throw ApiException.NotFound("Inventory was not found.");
        var increase = request.Type switch
        {
            StockMovementType.Restock or StockMovementType.Return => true,
            StockMovementType.Sale or StockMovementType.Damaged => false,
            _ => request.Increase
        };
        var change = increase ? request.Quantity : -request.Quantity;
        if (inventory.Quantity + change < 0)
        {
            throw ApiException.Conflict("Inventory adjustment would make stock negative.");
        }

        inventory.Quantity += change;
        inventory.UpdatedAt = timeProvider.GetUtcNow();
        db.StockMovements.Add(new StockMovement
        {
            InventoryId = inventory.Id,
            Inventory = inventory,
            QuantityChange = change,
            Type = request.Type,
            Reason = request.Reason.Trim(),
            PerformedByUserId = adminId,
            CreatedAt = timeProvider.GetUtcNow()
        });
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(inventory);
    }

    public async Task<IReadOnlyList<StockMovementResponse>> HistoryAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!await db.Inventories.AnyAsync(inventory => inventory.ProductId == productId, cancellationToken))
        {
            throw ApiException.NotFound("Inventory was not found.");
        }

        return await db.StockMovements.AsNoTracking()
            .Where(movement => movement.Inventory.ProductId == productId)
            .OrderByDescending(movement => movement.CreatedAt)
            .Select(movement => new StockMovementResponse(
                movement.Id,
                movement.QuantityChange,
                movement.Type,
                movement.Reason,
                movement.PerformedByUserId,
                movement.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static InventoryResponse ToResponse(Inventory inventory) => new(
        inventory.ProductId,
        inventory.Product.Name,
        inventory.Product.Sku,
        inventory.Quantity,
        inventory.MinimumStockLevel,
        inventory.Quantity <= inventory.MinimumStockLevel,
        inventory.UpdatedAt);
}

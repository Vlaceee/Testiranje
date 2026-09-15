using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class DashboardService(ForgeMartDbContext db, TimeProvider timeProvider)
{
    public async Task<DashboardResponse> GetAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var rangeTo = to ?? now;
        var rangeFrom = from ?? rangeTo.AddDays(-30);
        if (rangeFrom > rangeTo || rangeTo - rangeFrom > TimeSpan.FromDays(366))
        {
            throw ApiException.BadRequest("Dashboard date range must be ordered and no longer than 366 days.");
        }

        var orders = await db.Orders.AsNoTracking()
            .Where(order => order.CreatedAt >= rangeFrom && order.CreatedAt <= rangeTo && order.Status != OrderStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var revenue = orders.Where(order => order.PaymentStatus == PaymentStatus.Paid).Sum(order => order.GrandTotal);
        var cost = orders.Where(order => order.PaymentStatus == PaymentStatus.Paid).Sum(order => order.CostTotal);
        var refunded = orders.Where(order => order.PaymentStatus == PaymentStatus.Refunded).Sum(order => order.GrandTotal);
        var lowStock = await db.Inventories.AsNoTracking().Include(inventory => inventory.Product)
            .Where(inventory => inventory.Quantity <= inventory.MinimumStockLevel)
            .OrderBy(inventory => inventory.Quantity)
            .Select(inventory => new InventoryResponse(
                inventory.ProductId,
                inventory.Product.Name,
                inventory.Product.Sku,
                inventory.Quantity,
                inventory.MinimumStockLevel,
                true,
                inventory.UpdatedAt))
            .ToListAsync(cancellationToken);
        var series = orders.Where(order => order.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(order => DateOnly.FromDateTime(order.CreatedAt.UtcDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new RevenuePointResponse(group.Key, group.Sum(order => order.GrandTotal), group.Sum(order => order.CostTotal)))
            .ToArray();
        var paidOrderCount = orders.Count(order => order.PaymentStatus == PaymentStatus.Paid);

        return new DashboardResponse(
            rangeFrom,
            rangeTo,
            revenue,
            cost,
            revenue - cost,
            refunded,
            revenue - refunded,
            revenue - refunded - cost,
            orders.Count,
            paidOrderCount == 0 ? 0m : decimal.Round(revenue / paidOrderCount, 2),
            await db.OrderItems.AsNoTracking()
                .Where(item => item.Order.CreatedAt >= rangeFrom && item.Order.CreatedAt <= rangeTo && item.Order.Status != OrderStatus.Cancelled)
                .SumAsync(item => (decimal?)item.Quantity, cancellationToken) ?? 0m,
            lowStock,
            series);
    }
}


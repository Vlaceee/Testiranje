using System.ComponentModel.DataAnnotations;
using ForgeMart.Api.Domain;

namespace ForgeMart.Api.Contracts;

public sealed record InventoryResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal Quantity,
    decimal MinimumStockLevel,
    bool IsLowStock,
    DateTimeOffset UpdatedAt);

public sealed record InventoryAdjustmentRequest(
    [Range(typeof(decimal), "0.001", "100000000")] decimal Quantity,
    [EnumDataType(typeof(StockMovementType))] StockMovementType Type,
    [Required, MinLength(3), MaxLength(300)] string Reason,
    bool Increase = true);

public sealed record StockMovementResponse(
    Guid Id,
    decimal QuantityChange,
    StockMovementType Type,
    string Reason,
    Guid? PerformedByUserId,
    DateTimeOffset CreatedAt);

public sealed record DashboardResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal GrossProfit,
    decimal RefundedAmount,
    decimal NetRevenue,
    decimal NetProfit,
    int NumberOfOrders,
    decimal AverageOrderValue,
    decimal ProductsSold,
    IReadOnlyList<InventoryResponse> LowStockProducts,
    IReadOnlyList<RevenuePointResponse> RevenueSeries);

public sealed record RevenuePointResponse(DateOnly Date, decimal Revenue, decimal Cost);

public sealed record UserAdminResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record SetUserActiveRequest(bool IsActive);

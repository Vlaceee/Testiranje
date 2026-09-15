using System.ComponentModel.DataAnnotations;
using ForgeMart.Api.Domain;

namespace ForgeMart.Api.Contracts;

public sealed class CheckoutRequest
{
    [Required, MinLength(3), MaxLength(200)]
    public required string ContactName { get; init; }

    [Required, EmailAddress, MaxLength(254)]
    public required string ContactEmail { get; init; }

    [Required, MinLength(5), MaxLength(300)]
    public required string ShippingAddress { get; init; }

    [Required, MinLength(2), MaxLength(100)]
    public required string ShippingCity { get; init; }

    [Required, MinLength(3), MaxLength(20)]
    public required string ShippingPostalCode { get; init; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod PaymentMethod { get; init; }

    [CreditCard]
    public string? MockCardNumber { get; init; }
}

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    string CustomerName,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt,
    decimal Subtotal,
    decimal VatTotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    decimal CostTotal,
    string ContactName,
    string ContactEmail,
    string ShippingAddress,
    string ShippingCity,
    string ShippingPostalCode,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal Quantity,
    UnitOfMeasure UnitOfMeasure,
    decimal UnitPrice,
    decimal VatRate,
    decimal DiscountPercentage,
    decimal LineSubtotal,
    decimal LineVat,
    decimal LineTotal);

public sealed record UpdateOrderStatusRequest(
    [EnumDataType(typeof(OrderStatus))] OrderStatus Status);

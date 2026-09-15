using System.ComponentModel.DataAnnotations;
using ForgeMart.Api.Domain;

namespace ForgeMart.Api.Contracts;

public sealed record CartResponse(
    Guid Id,
    IReadOnlyList<CartItemResponse> Items,
    int DistinctItemCount,
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    DateTimeOffset UpdatedAt);

public sealed record CartItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Sku,
    string ImageUrl,
    UnitOfMeasure UnitOfMeasure,
    decimal Quantity,
    decimal AvailableStock,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineSubtotal,
    decimal LineVat,
    decimal LineTotal,
    bool ProductIsActive);

public sealed record SetCartItemRequest(
    [Range(typeof(decimal), "0.001", "100000")] decimal Quantity);

public sealed record AnonymousCartItemRequest(
    Guid ProductId,
    [Range(typeof(decimal), "0.001", "100000")] decimal Quantity);

public sealed record MergeCartRequest(
    [Required, MinLength(1)] IReadOnlyList<AnonymousCartItemRequest> Items);

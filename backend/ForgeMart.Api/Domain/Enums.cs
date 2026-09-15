using System.Text.Json.Serialization;

namespace ForgeMart.Api.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Customer,
    Admin
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnitOfMeasure
{
    Piece,
    Meter,
    Pack,
    Kilogram,
    SquareMeter
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StockMovementType
{
    Restock,
    Sale,
    Return,
    Damaged,
    ManualAdjustment
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    PendingPayment,
    Paid,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Pending,
    Paid,
    Declined,
    Refunded
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    CashOnDelivery,
    MockCard
}

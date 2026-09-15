using System.ComponentModel.DataAnnotations;
using ForgeMart.Api.Domain;

namespace ForgeMart.Api.Contracts;

public sealed class ProductQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [MaxLength(150)]
    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    [MaxLength(100)]
    public string? Brand { get; init; }

    [Range(typeof(decimal), "0", "10000000")]
    public decimal? MinPrice { get; init; }

    [Range(typeof(decimal), "0", "10000000")]
    public decimal? MaxPrice { get; init; }

    public bool? InStock { get; init; }
    public bool? Discounted { get; init; }
    public bool IncludeInactive { get; init; }
    public ProductSort Sort { get; init; } = ProductSort.NameAsc;
}

public enum ProductSort
{
    NameAsc,
    NameDesc,
    PriceAsc,
    PriceDesc,
    Newest,
    MostSold
}

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Sku,
    string Brand,
    Guid CategoryId,
    string CategoryName,
    decimal Price,
    decimal CurrentPrice,
    decimal VatRate,
    decimal DiscountPercentage,
    bool HasActiveDiscount,
    UnitOfMeasure UnitOfMeasure,
    int UnitsPerPackage,
    decimal StockQuantity,
    string ImageUrl,
    bool IsActive);

public sealed record ProductDetailResponse(
    Guid Id,
    string Name,
    string Sku,
    string Description,
    string Brand,
    Guid CategoryId,
    string CategoryName,
    decimal Price,
    decimal CostPrice,
    decimal CurrentPrice,
    decimal VatRate,
    decimal DiscountPercentage,
    DateTimeOffset? DiscountStart,
    DateTimeOffset? DiscountEnd,
    UnitOfMeasure UnitOfMeasure,
    int UnitsPerPackage,
    decimal StockQuantity,
    decimal MinimumStockLevel,
    string ImageUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class UpsertProductRequest : IValidatableObject
{
    [Required, MinLength(2), MaxLength(150)]
    public required string Name { get; init; }

    [Required, MinLength(3), MaxLength(30), RegularExpression("^[A-Za-z0-9-]+$")]
    public required string Sku { get; init; }

    [Required, MinLength(10), MaxLength(2000)]
    public required string Description { get; init; }

    [Required, MinLength(2), MaxLength(100)]
    public required string Brand { get; init; }

    public Guid CategoryId { get; init; }

    [Range(typeof(decimal), "0.01", "10000000")]
    public decimal Price { get; init; }

    [Range(typeof(decimal), "0", "10000000")]
    public decimal CostPrice { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal VatRate { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercentage { get; init; }

    public DateTimeOffset? DiscountStart { get; init; }
    public DateTimeOffset? DiscountEnd { get; init; }

    [EnumDataType(typeof(UnitOfMeasure))]
    public UnitOfMeasure UnitOfMeasure { get; init; }

    [Range(1, 100000)]
    public int UnitsPerPackage { get; init; } = 1;

    [Required, MaxLength(500)]
    public required string ImageUrl { get; init; }

    [Range(typeof(decimal), "0", "100000000")]
    public decimal InitialStockQuantity { get; init; }

    [Range(typeof(decimal), "0", "100000000")]
    public decimal MinimumStockLevel { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CostPrice > Price)
        {
            yield return new ValidationResult("Cost price cannot exceed selling price.", [nameof(CostPrice)]);
        }

        if (DiscountStart.HasValue && DiscountEnd.HasValue && DiscountStart > DiscountEnd)
        {
            yield return new ValidationResult("Discount start must not be after discount end.", [nameof(DiscountStart), nameof(DiscountEnd)]);
        }

        if (UnitOfMeasure != UnitOfMeasure.Pack && UnitsPerPackage != 1)
        {
            yield return new ValidationResult("Units per package must be 1 unless the unit is Pack.", [nameof(UnitsPerPackage)]);
        }
    }
}


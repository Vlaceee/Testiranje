using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class ProductService(ForgeMartDbContext db, TimeProvider timeProvider)
{
    public async Task<PagedResult<ProductResponse>> SearchAsync(
        ProductQuery request,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
        {
            throw ApiException.BadRequest("Minimum price cannot exceed maximum price.");
        }

        var now = timeProvider.GetUtcNow();
        var query = db.Products.AsNoTracking().Include(product => product.Category).Include(product => product.Inventory).AsQueryable();

        if (!isAdmin || !request.IncludeInactive)
        {
            query = query.Where(product => product.IsActive && product.Category.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{CourseworkBaselineRules.NormalizeSearch(request.Search)}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Sku, pattern) ||
                EF.Functions.ILike(product.Brand, pattern) ||
                EF.Functions.ILike(product.Description, pattern));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            query = query.Where(product => EF.Functions.ILike(product.Brand, request.Brand.Trim()));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= request.MinPrice);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= request.MaxPrice);
        }

        if (request.InStock.HasValue)
        {
            query = request.InStock.Value
                ? query.Where(product => product.Inventory.Quantity > 0)
                : query.Where(product => product.Inventory.Quantity <= 0);
        }

        if (request.Discounted.HasValue)
        {
            var discounted = query.Where(product =>
                product.DiscountPercentage > 0 &&
                (!product.DiscountStart.HasValue || product.DiscountStart <= now) &&
                (!product.DiscountEnd.HasValue || product.DiscountEnd >= now));
            query = request.Discounted.Value ? discounted : query.Except(discounted);
        }

        query = request.Sort switch
        {
            ProductSort.NameDesc => query.OrderByDescending(product => product.Name),
            ProductSort.PriceAsc => query.OrderBy(product => product.Price),
            ProductSort.PriceDesc => query.OrderByDescending(product => product.Price),
            ProductSort.Newest => query.OrderByDescending(product => product.CreatedAt),
            ProductSort.MostSold => query.OrderByDescending(product => product.OrderItems.Sum(item => (decimal?)item.Quantity) ?? 0m),
            _ => query.OrderBy(product => product.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .Skip(CourseworkBaselineRules.ProductPageOffset(request.Page, request.PageSize))
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var totalPages = CourseworkBaselineRules.TotalPages(totalCount, request.PageSize);

        return new PagedResult<ProductResponse>(
            products.Select(product => ToResponse(product, now)).ToArray(),
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<ProductDetailResponse> GetAsync(Guid id, bool isAdmin, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(candidate => candidate.Category)
            .Include(candidate => candidate.Inventory)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Product was not found.");

        if (!isAdmin && (!product.IsActive || !product.Category.IsActive))
        {
            throw ApiException.NotFound("Product was not found.");
        }

        return ToDetailResponse(product, timeProvider.GetUtcNow());
    }

    public async Task<ProductDetailResponse> CreateAsync(
        UpsertProductRequest request,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        await EnsureCategoryAsync(request.CategoryId, cancellationToken);
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(product => product.Sku == normalizedSku, cancellationToken))
        {
            throw ApiException.Conflict("A product with this SKU already exists.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = normalizedSku,
            Description = request.Description.Trim(),
            Brand = request.Brand.Trim(),
            CategoryId = request.CategoryId,
            Price = request.Price,
            CostPrice = request.CostPrice,
            VatRate = request.VatRate,
            DiscountPercentage = request.DiscountPercentage,
            DiscountStart = request.DiscountStart,
            DiscountEnd = request.DiscountEnd,
            UnitOfMeasure = request.UnitOfMeasure,
            UnitsPerPackage = request.UnitsPerPackage,
            ImageUrl = request.ImageUrl.Trim(),
            Inventory = new Inventory
            {
                Quantity = request.InitialStockQuantity,
                MinimumStockLevel = request.MinimumStockLevel
            }
        };
        if (request.InitialStockQuantity > 0)
        {
            product.Inventory.Movements.Add(new StockMovement
            {
                QuantityChange = request.InitialStockQuantity,
                Type = StockMovementType.Restock,
                Reason = "Initial product stock",
                PerformedByUserId = adminId
            });
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(product).Reference(candidate => candidate.Category).LoadAsync(cancellationToken);
        return ToDetailResponse(product, timeProvider.GetUtcNow());
    }

    public async Task<ProductDetailResponse> UpdateAsync(
        Guid id,
        UpsertProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Include(candidate => candidate.Category)
            .Include(candidate => candidate.Inventory)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Product was not found.");
        await EnsureCategoryAsync(request.CategoryId, cancellationToken);

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(candidate => candidate.Id != id && candidate.Sku == normalizedSku, cancellationToken))
        {
            throw ApiException.Conflict("A product with this SKU already exists.");
        }

        product.Name = request.Name.Trim();
        product.Sku = normalizedSku;
        product.Description = request.Description.Trim();
        product.Brand = request.Brand.Trim();
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.CostPrice = request.CostPrice;
        product.VatRate = request.VatRate;
        product.DiscountPercentage = request.DiscountPercentage;
        product.DiscountStart = request.DiscountStart;
        product.DiscountEnd = request.DiscountEnd;
        product.UnitOfMeasure = request.UnitOfMeasure;
        product.UnitsPerPackage = request.UnitsPerPackage;
        product.ImageUrl = request.ImageUrl.Trim();
        product.Inventory.MinimumStockLevel = request.MinimumStockLevel;
        product.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(product).Reference(candidate => candidate.Category).LoadAsync(cancellationToken);
        return ToDetailResponse(product, timeProvider.GetUtcNow());
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Product was not found.");
        if (product.IsActive == isActive)
        {
            throw ApiException.Conflict(isActive ? "Product is already active." : "Product is already inactive.");
        }

        product.IsActive = isActive;
        product.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await db.Categories.AnyAsync(category => category.Id == categoryId && category.IsActive, cancellationToken))
        {
            throw ApiException.BadRequest("An active category is required.");
        }
    }

    public static ProductResponse ToResponse(Product product, DateTimeOffset now) => new(
        product.Id,
        product.Name,
        product.Sku,
        product.Brand,
        product.CategoryId,
        product.Category.Name,
        product.Price,
        product.CurrentNetPrice(now),
        product.VatRate,
        product.DiscountPercentage,
        product.HasActiveDiscount(now),
        product.UnitOfMeasure,
        product.UnitsPerPackage,
        product.Inventory.Quantity,
        product.ImageUrl,
        product.IsActive);

    private static ProductDetailResponse ToDetailResponse(Product product, DateTimeOffset now) => new(
        product.Id,
        product.Name,
        product.Sku,
        product.Description,
        product.Brand,
        product.CategoryId,
        product.Category.Name,
        product.Price,
        product.CostPrice,
        product.CurrentNetPrice(now),
        product.VatRate,
        product.DiscountPercentage,
        product.DiscountStart,
        product.DiscountEnd,
        product.UnitOfMeasure,
        product.UnitsPerPackage,
        product.Inventory.Quantity,
        product.Inventory.MinimumStockLevel,
        product.ImageUrl,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt);
}

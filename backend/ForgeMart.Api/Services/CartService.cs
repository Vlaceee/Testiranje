using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class CartService(ForgeMartDbContext db, TimeProvider timeProvider)
{
    public async Task<CartResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await LoadCartAsync(userId, cancellationToken);
        return ToResponse(cart, timeProvider.GetUtcNow());
    }

    public async Task<CartResponse> SetItemAsync(
        Guid userId,
        Guid productId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var cart = await LoadCartAsync(userId, cancellationToken);
        var product = await db.Products.Include(candidate => candidate.Inventory)
            .SingleOrDefaultAsync(candidate => candidate.Id == productId && candidate.IsActive, cancellationToken)
            ?? throw ApiException.NotFound("Active product was not found.");
        CartRules.ValidateQuantity(quantity, product.Inventory.Quantity, product.UnitOfMeasure);

        var item = cart.Items.SingleOrDefault(candidate => candidate.ProductId == productId);
        if (item is null)
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                Cart = cart,
                ProductId = productId,
                Product = product,
                Quantity = quantity
            });
        }
        else
        {
            item.Quantity = quantity;
        }

        cart.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(cart, timeProvider.GetUtcNow());
    }

    public async Task<CartResponse> RemoveItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken)
    {
        var cart = await LoadCartAsync(userId, cancellationToken);
        var item = cart.Items.SingleOrDefault(candidate => candidate.ProductId == productId)
            ?? throw ApiException.NotFound("Cart item was not found.");
        db.CartItems.Remove(item);
        cart.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(cart, timeProvider.GetUtcNow());
    }

    public async Task<CartResponse> MergeAsync(Guid userId, MergeCartRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
        {
            throw ApiException.BadRequest("Anonymous cart merge payload must contain unique product IDs.");
        }

        var cart = await LoadCartAsync(userId, cancellationToken);
        var products = await db.Products.Include(product => product.Inventory)
            .Where(product => request.Items.Select(item => item.ProductId).Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var anonymousItem in request.Items)
        {
            if (!products.TryGetValue(anonymousItem.ProductId, out var product) || !product.IsActive)
            {
                throw ApiException.Conflict($"Product {anonymousItem.ProductId} is no longer available.");
            }

            var serverItem = cart.Items.SingleOrDefault(item => item.ProductId == anonymousItem.ProductId);
            var mergedQuantity = anonymousItem.Quantity + (serverItem?.Quantity ?? 0m);
            CartRules.ValidateQuantity(mergedQuantity, product.Inventory.Quantity, product.UnitOfMeasure);
            if (serverItem is null)
            {
                db.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    Cart = cart,
                    ProductId = product.Id,
                    Product = product,
                    Quantity = mergedQuantity
                });
            }
            else
            {
                serverItem.Quantity = mergedQuantity;
            }
        }

        cart.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(cart, timeProvider.GetUtcNow());
    }

    private async Task<Cart> LoadCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await db.Carts
            .Include(candidate => candidate.Items)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Inventory)
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public static CartResponse ToResponse(Cart cart, DateTimeOffset now)
    {
        var items = cart.Items.OrderBy(item => item.Product.Name).Select(item =>
        {
            var netPrice = item.Product.CurrentNetPrice(now);
            var lineSubtotal = PriceCalculator.RoundMoney(netPrice * item.Quantity);
            var lineVat = PriceCalculator.Vat(lineSubtotal, item.Product.VatRate);
            return new CartItemResponse(
                item.Id,
                item.ProductId,
                item.Product.Name,
                item.Product.Sku,
                item.Product.ImageUrl,
                item.Product.UnitOfMeasure,
                item.Quantity,
                item.Product.Inventory.Quantity,
                netPrice,
                item.Product.VatRate,
                lineSubtotal,
                lineVat,
                lineSubtotal + lineVat,
                item.Product.IsActive);
        }).ToArray();

        return new CartResponse(
            cart.Id,
            items,
            items.Length,
            items.Sum(item => item.LineSubtotal),
            items.Sum(item => item.LineVat),
            items.Sum(item => item.LineTotal),
            cart.UpdatedAt);
    }
}

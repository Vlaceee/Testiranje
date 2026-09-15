using ForgeMart.Api.Contracts;
using ForgeMart.Api.Data;
using ForgeMart.Api.Domain;
using ForgeMart.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Services;

public sealed class OrderService(
    ForgeMartDbContext db,
    TimeProvider timeProvider,
    ICheckoutConcurrencyHook concurrencyHook,
    MockPaymentService paymentService)
{
    public async Task<OrderResponse> CheckoutAsync(Guid userId, CheckoutRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cart = await db.Carts
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Items)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Inventory)
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken)
            ?? throw ApiException.BadRequest("Cart is empty.");
        if (cart.Items.Count == 0)
        {
            throw ApiException.BadRequest("Cart is empty.");
        }

        var paid = paymentService.IsPaid(request);
        foreach (var cartItem in cart.Items)
        {
            if (!cartItem.Product.IsActive)
            {
                throw ApiException.Conflict($"Product {cartItem.Product.Name} is no longer active.");
            }

            if (cartItem.Quantity <= 0 || cartItem.Quantity > cartItem.Product.Inventory.Quantity)
            {
                throw ApiException.Conflict($"Insufficient stock for {cartItem.Product.Name}.");
            }
        }

        await concurrencyHook.AfterInventoryReadAsync(cancellationToken);

        var order = new Order
        {
            OrderNumber = $"FM-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            UserId = userId,
            Status = paid ? OrderStatus.Paid : OrderStatus.PendingPayment,
            PaymentStatus = paid ? PaymentStatus.Paid : PaymentStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = now,
            PaidAt = paid ? now : null,
            ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
            ShippingAddress = request.ShippingAddress.Trim(),
            ShippingCity = request.ShippingCity.Trim(),
            ShippingPostalCode = request.ShippingPostalCode.Trim()
        };

        foreach (var cartItem in cart.Items)
        {
            var product = cartItem.Product;
            var unitPrice = product.CurrentNetPrice(now);
            var lineSubtotal = PriceCalculator.RoundMoney(unitPrice * cartItem.Quantity);
            var lineVat = PriceCalculator.Vat(lineSubtotal, product.VatRate);
            var lineTotal = lineSubtotal + lineVat;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                SkuSnapshot = product.Sku,
                Quantity = cartItem.Quantity,
                UnitOfMeasure = product.UnitOfMeasure,
                UnitPrice = unitPrice,
                UnitCost = product.CostPrice,
                VatRate = product.VatRate,
                DiscountPercentage = product.HasActiveDiscount(now) ? product.DiscountPercentage : 0m,
                LineSubtotal = lineSubtotal,
                LineVat = lineVat,
                LineTotal = lineTotal
            });
            order.DiscountTotal += PriceCalculator.RoundMoney((product.Price - unitPrice) * cartItem.Quantity);

            // INTENTIONAL DEFECT: DEFECT-001
            // Educational purpose: demonstrates a lost-update/check-then-act race during concurrent checkout.
            // Expected failing test: ConcurrentCheckout_ShouldNotOversellLastItem.
            // Correct production behavior: an atomic conditional update or concurrency token allows only one buyer.
            // Coursework baseline: DO NOT FIX unless explicitly instructed to produce the corrected version.
            product.Inventory.Quantity -= cartItem.Quantity;
            product.Inventory.UpdatedAt = now;
            db.StockMovements.Add(new StockMovement
            {
                InventoryId = product.Inventory.Id,
                Inventory = product.Inventory,
                QuantityChange = -cartItem.Quantity,
                Type = StockMovementType.Sale,
                Reason = $"Checkout {order.OrderNumber}",
                PerformedByUserId = userId,
                CreatedAt = now
            });
        }

        order.Subtotal = order.Items.Sum(item => item.LineSubtotal);
        order.VatTotal = order.Items.Sum(item => item.LineVat);
        order.GrandTotal = order.Items.Sum(item => item.LineTotal);
        order.CostTotal = order.Items.Sum(item => item.UnitCost * item.Quantity);
        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(order);
    }

    public async Task<PagedResult<OrderResponse>> ListAsync(
        Guid userId,
        bool isAdmin,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw ApiException.BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        var query = db.Orders.AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Items)
            .Where(order => isAdmin || order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<OrderResponse>(
            orders.Select(ToResponse).ToArray(),
            page,
            pageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<OrderResponse> GetAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken)
            ?? throw ApiException.NotFound("Order was not found.");
        if (!isAdmin && order.UserId != userId)
        {
            throw ApiException.NotFound("Order was not found.");
        }

        return ToResponse(order);
    }

    public async Task<OrderResponse> CancelAsync(Guid orderId, Guid userId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order.UserId != userId)
        {
            throw ApiException.NotFound("Order was not found.");
        }

        if (!OrderStateMachine.CanCustomerCancel(order.Status, order.PaymentStatus))
        {
            throw ApiException.Conflict("A customer can cancel only an unpaid pending order.");
        }

        order.Status = OrderStatus.Cancelled;
        await RestoreInventoryAsync(order, userId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(order);
    }

    public async Task<OrderResponse> UpdateStatusAsync(Guid orderId, OrderStatus target, Guid adminId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (!OrderStateMachine.CanAdminTransition(order.Status, target))
        {
            throw ApiException.Conflict($"Transition from {order.Status} to {target} is not allowed.");
        }

        var now = timeProvider.GetUtcNow();
        order.Status = target;
        if (target == OrderStatus.Paid)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaidAt = now;
        }
        else if (target == OrderStatus.Shipped)
        {
            order.ShippedAt = now;
        }
        else if (target == OrderStatus.Delivered)
        {
            order.DeliveredAt = now;
        }
        else if (target == OrderStatus.Cancelled)
        {
            await RestoreInventoryAsync(order, adminId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(order);
    }

    private async Task<Order> LoadOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Orders.Include(order => order.User).Include(order => order.Items)
            .ThenInclude(item => item.Product).ThenInclude(product => product.Inventory)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken)
        ?? throw ApiException.NotFound("Order was not found.");

    private Task RestoreInventoryAsync(Order order, Guid performedBy, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            item.Product.Inventory.Quantity += item.Quantity;
            item.Product.Inventory.UpdatedAt = timeProvider.GetUtcNow();
            db.StockMovements.Add(new StockMovement
            {
                InventoryId = item.Product.Inventory.Id,
                Inventory = item.Product.Inventory,
                QuantityChange = item.Quantity,
                Type = StockMovementType.Return,
                Reason = $"Cancelled order {order.OrderNumber}",
                PerformedByUserId = performedBy,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }

        return Task.CompletedTask;
    }

    public static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.UserId,
        order.User is null ? string.Empty : $"{order.User.FirstName} {order.User.LastName}",
        order.Status,
        order.PaymentStatus,
        order.PaymentMethod,
        order.CreatedAt,
        order.PaidAt,
        order.ShippedAt,
        order.DeliveredAt,
        order.Subtotal,
        order.VatTotal,
        order.DiscountTotal,
        order.GrandTotal,
        order.CostTotal,
        order.ContactName,
        order.ContactEmail,
        order.ShippingAddress,
        order.ShippingCity,
        order.ShippingPostalCode,
        order.Items.Select(item => new OrderItemResponse(
            item.ProductId,
            item.ProductNameSnapshot,
            item.SkuSnapshot,
            item.Quantity,
            item.UnitOfMeasure,
            item.UnitPrice,
            item.VatRate,
            item.DiscountPercentage,
            item.LineSubtotal,
            item.LineVat,
            item.LineTotal)).ToArray());
}

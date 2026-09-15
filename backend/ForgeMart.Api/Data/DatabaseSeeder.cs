using ForgeMart.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Data;

public sealed class DatabaseSeeder(ForgeMartDbContext db, IPasswordHasher<User> passwordHasher)
{
    private static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid CustomerId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            await SeedHistoricalOrdersAsync(cancellationToken);
            return;
        }

        var admin = NewUser(AdminId, "admin@forgemart.test", "Admin", "User", UserRole.Admin);
        var customer = NewUser(CustomerId, "customer@forgemart.test", "Mila", "Petrovic", UserRole.Customer);
        var secondCustomer = NewUser(Guid.Parse("10000000-0000-0000-0000-000000000003"), "marko@forgemart.test", "Marko", "Jovanovic", UserRole.Customer);
        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");
        customer.PasswordHash = passwordHasher.HashPassword(customer, "Customer123!");
        secondCustomer.PasswordHash = passwordHasher.HashPassword(secondCustomer, "Customer123!");

        var categories = CreateCategories();
        var products = CreateProducts(categories);
        var inventories = products.Select((product, index) => new Inventory
        {
            Id = Guid.Parse($"30000000-0000-0000-0000-{index + 1:000000000000}"),
            ProductId = product.Id,
            Quantity = StockFor(index),
            MinimumStockLevel = index % 6 == 0 ? 5m : 3m
        }).ToArray();

        var movements = inventories.Select(inventory => new StockMovement
        {
            InventoryId = inventory.Id,
            QuantityChange = inventory.Quantity,
            Type = StockMovementType.Restock,
            Reason = "Deterministic initial seed",
            PerformedByUserId = AdminId
        });

        db.Users.AddRange(admin, customer, secondCustomer);
        db.Categories.AddRange(categories);
        db.Products.AddRange(products);
        db.Inventories.AddRange(inventories);
        db.StockMovements.AddRange(movements);
        db.Carts.AddRange(
            new Cart { UserId = CustomerId },
            new Cart { UserId = secondCustomer.Id });

        await db.SaveChangesAsync(cancellationToken);
        await SeedHistoricalOrdersAsync(cancellationToken);
    }

    private async Task SeedHistoricalOrdersAsync(CancellationToken cancellationToken)
    {
        if (await db.Orders.AnyAsync(order => order.OrderNumber.StartsWith("FM-SEED-"), cancellationToken))
        {
            return;
        }

        var products = await db.Products.OrderBy(product => product.Sku).Take(4).ToArrayAsync(cancellationToken);
        if (products.Length < 4)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var definitions = new[]
        {
            ("FM-SEED-DELIVERED", CustomerId, OrderStatus.Delivered, PaymentStatus.Paid, 12, 0),
            ("FM-SEED-PROCESSING", Guid.Parse("10000000-0000-0000-0000-000000000003"), OrderStatus.Processing, PaymentStatus.Paid, 4, 1),
            ("FM-SEED-PENDING", CustomerId, OrderStatus.PendingPayment, PaymentStatus.Pending, 1, 2),
            ("FM-SEED-CANCELLED", Guid.Parse("10000000-0000-0000-0000-000000000003"), OrderStatus.Cancelled, PaymentStatus.Pending, 20, 3)
        };

        foreach (var definition in definitions)
        {
            var product = products[definition.Item6];
            var createdAt = now.AddDays(-definition.Item5);
            var subtotal = PriceCalculator.RoundMoney(product.Price * 2m);
            var vat = PriceCalculator.Vat(subtotal, product.VatRate);
            var paid = definition.Item4 == PaymentStatus.Paid;
            var order = new Order
            {
                OrderNumber = definition.Item1,
                UserId = definition.Item2,
                Status = definition.Item3,
                PaymentStatus = definition.Item4,
                PaymentMethod = paid ? PaymentMethod.MockCard : PaymentMethod.CashOnDelivery,
                CreatedAt = createdAt,
                PaidAt = paid ? createdAt.AddMinutes(1) : null,
                ShippedAt = definition.Item3 is OrderStatus.Shipped or OrderStatus.Delivered ? createdAt.AddDays(1) : null,
                DeliveredAt = definition.Item3 == OrderStatus.Delivered ? createdAt.AddDays(3) : null,
                Subtotal = subtotal,
                VatTotal = vat,
                GrandTotal = subtotal + vat,
                CostTotal = PriceCalculator.RoundMoney(product.CostPrice * 2m),
                ContactName = definition.Item2 == CustomerId ? "Mila Petrovic" : "Marko Jovanovic",
                ContactEmail = definition.Item2 == CustomerId ? "customer@forgemart.test" : "marko@forgemart.test",
                ShippingAddress = "Seed Workshop Street 10",
                ShippingCity = "Nis",
                ShippingPostalCode = "18000"
            };
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                SkuSnapshot = product.Sku,
                Quantity = 2m,
                UnitOfMeasure = product.UnitOfMeasure,
                UnitPrice = product.Price,
                UnitCost = product.CostPrice,
                VatRate = product.VatRate,
                LineSubtotal = subtotal,
                LineVat = vat,
                LineTotal = subtotal + vat
            });
            db.Orders.Add(order);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User NewUser(Guid id, string email, string firstName, string lastName, UserRole role) => new()
    {
        Id = id,
        Email = email,
        PasswordHash = string.Empty,
        FirstName = firstName,
        LastName = lastName,
        Role = role
    };

    private static Category[] CreateCategories()
    {
        var definitions = new[]
        {
            ("Power Tools", "Professional and DIY electric tools"),
            ("Hand Tools", "Reliable workshop hand tools"),
            ("Fasteners", "Screws, nails, anchors, and fittings"),
            ("Lumber & Materials", "Boards, plywood, and building material"),
            ("Electrical", "Cables, sockets, and electrical accessories"),
            ("Plumbing", "Pipes, valves, and plumbing accessories"),
            ("Safety Equipment", "Workplace personal protective equipment"),
            ("Paint & Supplies", "Paint, brushes, rollers, and preparation supplies")
        };

        return definitions.Select((definition, index) => new Category
        {
            Id = Guid.Parse($"20000000-0000-0000-0000-{index + 1:000000000000}"),
            Name = definition.Item1,
            Description = definition.Item2
        }).ToArray();
    }

    private static Product[] CreateProducts(Category[] categories)
    {
        var names = new[]
        {
            "18V Cordless Drill", "125mm Angle Grinder", "Circular Saw 1200W", "Rotary Hammer", "Orbital Sander",
            "Claw Hammer", "Insulated Screwdriver Set", "Combination Pliers", "Adjustable Wrench", "Tape Measure 5m",
            "Box of 100 Wood Screws", "Galvanized Nails 3x60", "Wall Plug Pack", "Hex Bolt Pack", "Drywall Screws",
            "Pine Board 2m", "Birch Plywood Sheet", "OSB Board", "Construction Timber", "MDF Panel",
            "Copper Cable 3x2.5", "Extension Cord 20m", "Double Wall Socket", "LED Work Light", "Circuit Breaker 16A",
            "PVC Pipe 32mm", "Ball Valve 1/2", "Flexible Hose", "Pipe Wrench", "Thread Seal Tape",
            "Work Gloves", "Safety Goggles", "Protective Helmet", "Ear Defenders", "Dust Mask Pack",
            "Interior Paint 10L", "Exterior Paint 10L", "Paint Roller Set", "Brush Set", "Masking Tape"
        };
        var brands = new[] { "IronPeak", "VoltCraft", "Buildora", "NorthForge", "ProLine" };

        return names.Select((name, index) =>
        {
            var categoryIndex = index / 5;
            var unit = categoryIndex switch
            {
                2 => UnitOfMeasure.Pack,
                3 when index is 15 or 18 => UnitOfMeasure.Meter,
                4 when index == 20 => UnitOfMeasure.Meter,
                5 when index == 25 => UnitOfMeasure.Meter,
                _ => UnitOfMeasure.Piece
            };
            var price = 249m + (index * 537m);
            return new Product
            {
                Id = Guid.Parse($"40000000-0000-0000-0000-{index + 1:000000000000}"),
                Name = name,
                Sku = $"FM-{categoryIndex + 1:00}-{index + 1:000}",
                Description = $"Workshop-tested {name.ToLowerInvariant()} for builders, craftspeople, and DIY projects.",
                Brand = brands[index % brands.Length],
                CategoryId = categories[categoryIndex].Id,
                Price = price,
                CostPrice = decimal.Round(price * 0.62m, 2),
                VatRate = 20m,
                DiscountPercentage = index % 7 == 0 ? 15m : 0m,
                DiscountStart = index % 7 == 0 ? DateTimeOffset.UtcNow.AddDays(-2) : null,
                DiscountEnd = index % 7 == 0 ? DateTimeOffset.UtcNow.AddDays(30) : null,
                UnitOfMeasure = unit,
                UnitsPerPackage = unit == UnitOfMeasure.Pack ? 100 : 1,
                ImageUrl = $"/assets/products/product-{index + 1:00}.svg",
                IsActive = index is not 38 and not 39,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-index)
            };
        }).ToArray();
    }

    private static decimal StockFor(int index) => index switch
    {
        5 or 25 => 0m,
        10 => 1m,
        0 or 6 or 12 => 2m,
        _ => 10m + (index % 17)
    };
}

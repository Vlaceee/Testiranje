using ForgeMart.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ForgeMart.Api.Data;

public sealed class ForgeMartDbContext(DbContextOptions<ForgeMartDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("store");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(600);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.CategoryId });
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Sku).HasMaxLength(30);
            entity.Property(x => x.Brand).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Price).HasPrecision(12, 2);
            entity.Property(x => x.CostPrice).HasPrecision(12, 2);
            entity.Property(x => x.VatRate).HasPrecision(5, 2);
            entity.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(x => x.UnitOfMeasure).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasIndex(x => x.ProductId).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(14, 3);
            entity.Property(x => x.MinimumStockLevel).HasPrecision(14, 3);
            entity.HasOne(x => x.Product).WithOne(x => x.Inventory).HasForeignKey<Inventory>(x => x.ProductId);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(x => x.QuantityChange).HasPrecision(14, 3);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Reason).HasMaxLength(300);
            entity.HasOne(x => x.Inventory).WithMany(x => x.Movements).HasForeignKey(x => x.InventoryId);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne(x => x.User).WithOne(x => x.Cart).HasForeignKey<Cart>(x => x.UserId);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(14, 3);
            entity.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.Property(x => x.OrderNumber).HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Subtotal).HasPrecision(12, 2);
            entity.Property(x => x.VatTotal).HasPrecision(12, 2);
            entity.Property(x => x.DiscountTotal).HasPrecision(12, 2);
            entity.Property(x => x.GrandTotal).HasPrecision(12, 2);
            entity.Property(x => x.CostTotal).HasPrecision(12, 2);
            entity.Property(x => x.ContactName).HasMaxLength(200);
            entity.Property(x => x.ContactEmail).HasMaxLength(254);
            entity.Property(x => x.ShippingAddress).HasMaxLength(300);
            entity.Property(x => x.ShippingCity).HasMaxLength(100);
            entity.Property(x => x.ShippingPostalCode).HasMaxLength(20);
            entity.HasOne(x => x.User).WithMany(x => x.Orders).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(x => x.ProductNameSnapshot).HasMaxLength(150);
            entity.Property(x => x.SkuSnapshot).HasMaxLength(30);
            entity.Property(x => x.Quantity).HasPrecision(14, 3);
            entity.Property(x => x.UnitOfMeasure).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.UnitCost).HasPrecision(12, 2);
            entity.Property(x => x.VatRate).HasPrecision(5, 2);
            entity.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(x => x.LineSubtotal).HasPrecision(12, 2);
            entity.Property(x => x.LineVat).HasPrecision(12, 2);
            entity.Property(x => x.LineTotal).HasPrecision(12, 2);
            entity.HasOne(x => x.Product).WithMany(x => x.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}


using Microsoft.EntityFrameworkCore;
using Modules.Orders.Domain;

namespace Modules.Orders.Infrastructure.Data;

public class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Enforce Schema Segregation
        modelBuilder.HasDefaultSchema("orders");

        // 2. Map the Order Entity
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o => o.Status).IsRequired().HasMaxLength(50);

            // Strictly map the internal collection
            builder.HasMany(o => o.Items)
                   .WithOne()
                   .HasForeignKey(i => i.OrderId)
                   .OnDelete(DeleteBehavior.Cascade); // If order is deleted, delete items
        });

        // 3. Map the OrderItem Entity
        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.ProductId).IsRequired();
            builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Quantity).IsRequired();

            // Map the Money Value Object for the item price
            builder.OwnsOne(i => i.UnitPrice, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("UnitPriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                    
                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });
    }
}
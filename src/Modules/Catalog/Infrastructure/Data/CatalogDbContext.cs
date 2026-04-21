using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain.Products;
using Modules.Catalog.Domain.Reservation; // Add this
using Modules.Catalog.Domain.StockItems;  // Add this

namespace Modules.Catalog.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<StockItem> StockItems { get; set; }   // Added
    public DbSet<Reservation> Reservations { get; set; } // Added

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Enforce Schema Segregation
        modelBuilder.HasDefaultSchema("catalog");

        // 2. Map the Product Entity
        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).IsRequired().HasMaxLength(2000);

            // Map the Money Value Object (Owned Entity)
            builder.OwnsOne(p => p.Price, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("PriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                    
                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });

        // 3. Map the StockItem Entity
        modelBuilder.Entity<StockItem>(builder =>
        {
            builder.HasKey(s => s.Id);
            
            // We can treat the StockItem Id as the Product Id (1-to-1 relationship)
            builder.Property(s => s.AvailableQty).IsRequired();
            builder.Property(s => s.ReservedQty).IsRequired();
        });

        // 4. Map the Reservation Entity
        modelBuilder.Entity<Reservation>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ItemId).IsRequired();
            builder.Property(r => r.Quantity).IsRequired();
            
            // Store the Enum as a string in the database for readability
            builder.Property(r => r.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();
        });
    }
}
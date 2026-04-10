using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain.Products;

namespace Modules.Catalog.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

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

            // 3. Map the Money Value Object (Owned Entity)
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
    }
}
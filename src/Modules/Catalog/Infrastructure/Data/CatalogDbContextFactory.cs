using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Catalog.Infrastructure.Data;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new CatalogDbContext(optionsBuilder.Options);
    }
}
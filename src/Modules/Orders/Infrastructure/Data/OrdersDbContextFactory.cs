using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Orders.Infrastructure.Data;

public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new OrdersDbContext(optionsBuilder.Options);
    }
}
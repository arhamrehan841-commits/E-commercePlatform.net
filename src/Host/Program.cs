using Host.Extensions;
using Host.Middleware;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Application.Products.Create;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 2. Database Registration
var connectionString = builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<CatalogDbContext>(opt => opt.UseSqlServer(connectionString));
builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));

// 3. MediatR Orchestration
builder.Services.AddMediatR(config => 
{
    config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});

var app = builder.Build();

// 4. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations(); // <--- This triggers the Day 10 magic
}

app.UseExceptionHandler(); 
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
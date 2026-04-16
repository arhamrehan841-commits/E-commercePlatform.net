using Host.Extensions;
using SharedKernel.Dependency;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Application.Products.Create;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services
builder.Services.AddControllers();
builder.Services.AddGlobalExceptionHandler();

// 2. Adding Swagger for API documentation and testing
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

// 3. Database Registration
var connectionString = builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<CatalogDbContext>(opt => opt.UseSqlServer(connectionString));
builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));

// 4. MediatR Orchestration
builder.Services.AddMediatR(config => 
{
    config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});

var app = builder.Build();

// 5. MUST BE FIRST: Catch errors in everything below
app.UseExceptionHandler();

// 6. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   
    app.UseSwaggerUI();

    await app.ApplyMigrationsAsync(); // <--- This triggers the Day 10 magic
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
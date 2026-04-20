using SharedKernel.Database;
using BuildingBlocks.Dependency;
// Add these two new namespaces for your module entry points:
using Modules.Catalog.Infrastructure; 
using Modules.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services
builder.Services.AddControllers();
builder.Services.AddGlobalExceptionHandler();

// 2. Adding Swagger
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

// 3. Module Registration 
var connectionString = builder.Configuration.GetConnectionString("Database") 
    ?? throw new InvalidOperationException("CRITICAL: Database connection string is missing in appsettings.Development.json!");
builder.Services.AddCatalogModule(connectionString);
builder.Services.AddOrdersModule(connectionString);

var app = builder.Build();

// 5. MUST BE FIRST: Catch errors
app.UseExceptionHandler();

// 6. Pipeline Configuration (Dynamic Discovery)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   
    app.UseSwaggerUI();

    // Find all modules that have a database and migrate/seed them dynamically
    using var scope = app.Services.CreateScope();
    var modules = scope.ServiceProvider.GetServices<IModuleDatabase>();

    foreach (var module in modules)
    {
        await module.MigrateAsync();
    }

    foreach (var module in modules)
    {
        await module.SeedAsync();
    }
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
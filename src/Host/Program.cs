using Host.Middleware;
using Modules.Catalog.Application.Products.Create;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REGISTER SERVICES (Dependency Injection) ---

builder.Services.AddControllers();

// Register the Global Exception Handler infrastructure
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Register MediatR (This single line scans the Catalog assembly and finds all handlers)
builder.Services.AddMediatR(config => 
{
    config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
    // Note: We will add the Orders assembly here later!
});

var app = builder.Build();

// --- 2. CONFIGURE THE HTTP PIPELINE ---

// Activate the Exception Handler middleware first, so it wraps all incoming requests
app.UseExceptionHandler(); 

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
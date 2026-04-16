using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Exceptions;

namespace BuildingBlocks.Dependency;

public static class DependencyInjection
{
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        return services;
    }
}
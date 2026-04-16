using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Exceptions;

namespace SharedKernel.Dependency;

public static class DependencyInjection
{
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        return services;
    }
}
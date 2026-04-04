using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Host.Middleware;

// This class intercepts ANY unhandled exception thrown anywhere in the application
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 1. Securely log the full error and stack trace for internal debugging
        _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 2. Map the Exception type to a specific HTTP Status Code
        var statusCode = exception switch
        {
            // If the Domain throws a Guard Clause exception, it's a Bad Request (400)
            ArgumentException => StatusCodes.Status400BadRequest,
            
            // Otherwise, it's an unhandled Internal Server Error (500)
            _ => StatusCodes.Status500InternalServerError
        };

        // 3. Construct the RFC 7807 standard JSON response
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == 400 ? "Validation Error" : "Server Error",
            Detail = statusCode == 400 ? exception.Message : "An unexpected error occurred.",
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // 4. Return the secure response to the client
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to tell ASP.NET that we handled the exception and to stop processing
        return true; 
    }
}
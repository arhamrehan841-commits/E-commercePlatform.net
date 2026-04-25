using Microsoft.AspNetCore.Diagnostics; 
using Microsoft.AspNetCore.Http;        
using Microsoft.AspNetCore.Mvc;         
using Microsoft.Extensions.Logging;
using SharedKernel.Exceptions; // <-- We can use this safely now!

namespace BuildingBlocks.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
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
        _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 1. Clean, strongly-typed pattern matching
        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            StockValidationException => StatusCodes.Status422UnprocessableEntity, 
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch 
            {
                400 => "Validation Error",
                422 => "Order Unprocessable", 
                _ => "Server Error"
            },
            Detail = statusCode == 500 ? "An unexpected error occurred." : exception.Message,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // 2. Safe, strongly-typed cast
        if (exception is StockValidationException stockEx)
        {
            problemDetails.Extensions["rejections"] = stockEx.Rejections;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; 
    }
}
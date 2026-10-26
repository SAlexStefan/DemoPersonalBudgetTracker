using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PersonalBudgetTrackerApi.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
     private readonly ILogger<GlobalExceptionHandler> _logger;

       public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

       public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
       {
           var (status, title) = exception switch
           {
               UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
               KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
               _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
           };

           _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

           httpContext.Response.StatusCode = status;
           await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
           {
               Status = status,
               Title = title,
               Instance = httpContext.Request.Path
           }, ct);

           return true;
       }
}

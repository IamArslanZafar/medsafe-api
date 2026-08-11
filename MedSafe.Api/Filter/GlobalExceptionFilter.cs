using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedSafeAPI.Filter;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        var (statusCode, message) = context.Exception switch
        {
            ValidationException ex => (400, ex.Message),
            KeyNotFoundException ex => (404, ex.Message),
            UnauthorizedAccessException ex => (403, ex.Message),
            _ => (500, "An unexpected error occurred.")
        };

        if (statusCode == 500)
            _logger.LogError(context.Exception, "Unhandled exception: {Message}", context.Exception.Message);
        else
            _logger.LogWarning("{StatusCode} {ExceptionType}: {Message}", statusCode, context.Exception.GetType().Name, message);

        context.Result = new ObjectResult(new { message })
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}

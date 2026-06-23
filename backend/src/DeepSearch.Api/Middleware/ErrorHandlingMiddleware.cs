using DeepSearch.Core.Exceptions;

namespace DeepSearch.Api.Middleware;

/// <summary>
/// טיפול מרכזי בשגיאות לכל הבקשות:
/// - ValidationException -> 400 עם ההודעה הברורה למשתמש.
/// - כל שגיאה אחרת        -> 500 + לוג מלא של השגיאה (לא בולעים אותה!).
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteError(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while processing {Path}", context.Request.Path);
            await WriteError(context, StatusCodes.Status500InternalServerError, "אירעה שגיאה בלתי צפויה בשרת.");
        }
    }

    private static Task WriteError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { error = message });
    }
}
